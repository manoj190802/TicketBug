using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TicketBug.Backend.Services
{
    public class AiServiceResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = "Full Stack";
        public List<string> ExtractedSkills { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public int WordCount { get; set; }
    }

    public class AiServiceConnector
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiServiceUrl;

        public AiServiceConnector(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _aiServiceUrl = configuration.GetValue<string>("AiServiceSettings:Url") ?? "http://localhost:8000";
        }

        public async Task<AiServiceResponse> AnalyzeDocumentAsync(string fileName, Stream fileStream)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", fileName);

                var response = await _httpClient.PostAsync($"{_aiServiceUrl}/analyze", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<AiServiceResponse>(responseJson, options) ?? new AiServiceResponse();
                }

                var errorDetail = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI service returned status code {response.StatusCode}: {errorDetail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error contacting AI Service: {ex.Message}");
                
                // Fallback for simple .txt files if FastAPI is down
                if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    fileStream.Position = 0;
                    using var reader = new StreamReader(fileStream);
                    string text = await reader.ReadToEndAsync();
                    return LocalHeuristicAnalysis(fileName, text);
                }

                throw new Exception($"AI Analysis Service is offline. Please make sure the Python service is running at {_aiServiceUrl}. Detail: {ex.Message}");
            }
        }

        private AiServiceResponse LocalHeuristicAnalysis(string fileName, string text)
        {
            var textLower = text.ToLower();
            
            var frontendKeywords = new List<string> { "angular", "react", "vue", "html", "css", "typescript", "javascript", "js", "ts", "tailwind", "responsive", "ui", "ux" };
            var backendKeywords = new List<string> { "dotnet", "c#", "python", "fastapi", "django", "flask", "java", "spring", "node", "express", "mongodb", "sql", "postgres", "database", "db", "rest api" };

            var matchedFrontend = new List<string>();
            var matchedBackend = new List<string>();

            foreach (var kw in frontendKeywords)
            {
                if (textLower.Contains(kw)) matchedFrontend.Add(kw);
            }

            foreach (var kw in backendKeywords)
            {
                if (textLower.Contains(kw)) matchedBackend.Add(kw);
            }

            var skills = new List<string>();
            skills.AddRange(matchedFrontend);
            skills.AddRange(matchedBackend);

            string category = "Full Stack";
            if (matchedFrontend.Count > 0 && matchedBackend.Count == 0)
            {
                category = "Frontend";
            }
            else if (matchedBackend.Count > 0 && matchedFrontend.Count == 0)
            {
                category = "Backend";
            }
            else if (matchedFrontend.Count > 0 && matchedBackend.Count > 0)
            {
                category = "Full Stack";
            }

            return new AiServiceResponse
            {
                FileName = fileName,
                Category = category,
                ExtractedSkills = skills,
                Summary = text.Length > 200 ? text.Substring(0, 200) + "..." : text,
                WordCount = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length
            };
        }
    }
}
