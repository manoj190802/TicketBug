using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TicketBug.Backend.Models;
using TicketBug.Backend.Services;

namespace TicketBug.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevelopersController : ControllerBase
    {
        private readonly MongoDbService _dbService;

        public DevelopersController(MongoDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var devs = await _dbService.GetDevelopersAsync();
                return Ok(devs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var dev = await _dbService.GetDeveloperByIdAsync(id);
                if (dev == null) return NotFound($"Developer with ID {id} not found.");
                return Ok(dev);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Developer developer)
        {
            try
            {
                if (developer == null) return BadRequest("Developer data is null.");
                await _dbService.CreateDeveloperAsync(developer);
                return CreatedAtAction(nameof(GetById), new { id = developer.Id }, developer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Developer developer)
        {
            try
            {
                var existing = await _dbService.GetDeveloperByIdAsync(id);
                if (existing == null) return NotFound($"Developer with ID {id} not found.");

                await _dbService.UpdateDeveloperAsync(id, developer);
                return Ok(developer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var existing = await _dbService.GetDeveloperByIdAsync(id);
                if (existing == null) return NotFound($"Developer with ID {id} not found.");

                await _dbService.DeleteDeveloperAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
