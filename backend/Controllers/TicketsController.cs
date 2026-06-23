using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TicketBug.Backend.Models;
using TicketBug.Backend.Services;

namespace TicketBug.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly MongoDbService _dbService;
        private readonly AiServiceConnector _aiConnector;

        public TicketsController(MongoDbService dbService, AiServiceConnector aiConnector)
        {
            _dbService = dbService;
            _aiConnector = aiConnector;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var tickets = await _dbService.GetTicketsAsync();
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var history = await _dbService.GetHistoryAsync();
                // Sort history by date descending (newest first)
                var sortedHistory = history.OrderByDescending(h => h.AssignedAt).ToList();
                return Ok(sortedHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class AssignRequest
        {
            public string TicketId { get; set; } = string.Empty;
            public string DeveloperId { get; set; } = string.Empty;
            public double MatchScore { get; set; }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTicket([FromBody] AssignRequest request)
        {
            try
            {
                var ticket = await _dbService.GetTicketByIdAsync(request.TicketId);
                if (ticket == null) return NotFound($"Ticket {request.TicketId} not found.");

                var dev = await _dbService.GetDeveloperByIdAsync(request.DeveloperId);
                if (dev == null) return NotFound($"Developer {request.DeveloperId} not found.");

                // If ticket was already assigned to someone else, decrement their workload
                if (!string.IsNullOrEmpty(ticket.AssignedDeveloperId))
                {
                    var prevDev = await _dbService.GetDeveloperByIdAsync(ticket.AssignedDeveloperId);
                    if (prevDev != null)
                    {
                        prevDev.Workload = Math.Max(0, prevDev.Workload - 1);
                        await _dbService.UpdateDeveloperAsync(prevDev.Id!, prevDev);
                    }
                }

                // Increment new developer workload
                dev.Workload += 1;
                await _dbService.UpdateDeveloperAsync(dev.Id!, dev);

                // Update ticket details
                ticket.AssignedDeveloperId = dev.Id;
                ticket.AssignedDeveloperName = dev.Name;
                ticket.Status = "Assigned";
                await _dbService.UpdateTicketAsync(ticket.Id!, ticket);

                // Add to history log
                var historyEntry = new AssignmentHistory
                {
                    TicketId = ticket.Id!,
                    TicketTitle = ticket.Title,
                    DeveloperId = dev.Id!,
                    DeveloperName = dev.Name,
                    AssignedAt = DateTime.UtcNow,
                    MatchScore = request.MatchScore
                };
                await _dbService.CreateHistoryAsync(historyEntry);

                return Ok(new { Message = "Assignment successful", Ticket = ticket, Developer = dev });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAndAnalyze(IFormFile file, [FromForm] string title, [FromForm] string description)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("File is empty or null.");
                if (string.IsNullOrEmpty(title)) title = file.FileName;

                // Send to FastAPI AI service
                using var stream = file.OpenReadStream();
                var aiResponse = await _aiConnector.AnalyzeDocumentAsync(file.FileName, stream);

                // Create the ticket
                var ticket = new TaskTicket
                {
                    Title = title,
                    Description = string.IsNullOrEmpty(description) ? aiResponse.Summary : description,
                    Category = aiResponse.Category,
                    RequiredSkills = aiResponse.ExtractedSkills,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _dbService.CreateTicketAsync(ticket);

                // Get recommendations
                var recommendations = await GetDeveloperRecommendations(ticket);

                return Ok(new
                {
                    Ticket = ticket,
                    Recommendations = recommendations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error during analysis: {ex.Message}");
            }
        }

        private async Task<List<RecommendationResult>> GetDeveloperRecommendations(TaskTicket ticket)
        {
            var devs = await _dbService.GetDevelopersAsync();
            var recommendations = new List<RecommendationResult>();

            foreach (var dev in devs)
            {
                // Exclude developers who are on leave
                if (dev.AvailabilityStatus.Equals("On Leave", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                double skillScore = 0;
                var matchedSkills = new List<string>();

                if (ticket.RequiredSkills.Count > 0)
                {
                    var devSkillsSet = new HashSet<string>(dev.Skills.Select(s => s.ToLower()));
                    foreach (var skill in ticket.RequiredSkills)
                    {
                        if (devSkillsSet.Contains(skill.ToLower()))
                        {
                            matchedSkills.Add(skill);
                        }
                    }
                    skillScore = (double)matchedSkills.Count / ticket.RequiredSkills.Count * 50; // Max 50 points
                }
                else
                {
                    skillScore = 30; // Baseline skill match if no specific skills are parsed
                }

                // Category match: e.g. Frontend developer should do frontend tickets (max 20 points)
                double categoryScore = 0;
                bool isFrontendTicket = ticket.Category.Equals("Frontend", StringComparison.OrdinalIgnoreCase);
                bool isBackendTicket = ticket.Category.Equals("Backend", StringComparison.OrdinalIgnoreCase);
                bool isFullStackTicket = ticket.Category.Equals("Full Stack", StringComparison.OrdinalIgnoreCase);

                bool devHasFrontend = dev.Skills.Any(s => new[] { "angular", "react", "vue", "html", "css", "typescript", "javascript", "tailwind" }.Contains(s.ToLower()));
                bool devHasBackend = dev.Skills.Any(s => new[] { "dotnet", "c#", "python", "fastapi", "django", "flask", "java", "spring", "node", "mongodb", "sql", "postgres" }.Contains(s.ToLower()));

                if (isFrontendTicket && devHasFrontend && !devHasBackend) categoryScore = 20;
                else if (isBackendTicket && devHasBackend && !devHasFrontend) categoryScore = 20;
                else if (isFullStackTicket && devHasFrontend && devHasBackend) categoryScore = 20;
                else if (isFullStackTicket && (devHasFrontend || devHasBackend)) categoryScore = 15;
                else if (devHasFrontend || devHasBackend) categoryScore = 10;
                else categoryScore = 5;

                // Workload score: Lower workload gets higher score (max 20 points)
                double workloadScore = dev.Workload switch
                {
                    0 => 20,
                    1 => 17,
                    2 => 12,
                    3 => 7,
                    _ => 0
                };

                // Experience score: 1 point per year of experience, up to 10 points (max 10 points)
                double experienceScore = Math.Min(10, dev.Experience);

                double totalScore = Math.Round(skillScore + categoryScore + workloadScore + experienceScore, 1);

                // Explanation string
                string workloadDesc = dev.Workload switch
                {
                    0 => "no active tasks",
                    1 => "1 active task",
                    _ => $"{dev.Workload} active tasks"
                };

                string explanation = $"{dev.Name} has {matchedSkills.Count} of the {ticket.RequiredSkills.Count} required skills ({string.Join(", ", matchedSkills)}). They have {dev.Experience} years of experience and {workloadDesc}.";

                recommendations.Add(new RecommendationResult
                {
                    Developer = dev,
                    MatchScore = totalScore,
                    SkillsMatched = matchedSkills,
                    Explanation = explanation
                });
            }

            // Sort by match score descending
            return recommendations.OrderByDescending(r => r.MatchScore).ToList();
        }
    }

    public class RecommendationResult
    {
        public Developer Developer { get; set; } = null!;
        public double MatchScore { get; set; }
        public List<string> SkillsMatched { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
    }
}
