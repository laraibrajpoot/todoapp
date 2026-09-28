using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace todolist.Services
{
    public class AiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiSettings:ApiKey"] ?? string.Empty;
        }

        public async Task<string> GenerateTaskGuideAsync(string taskTitle)
        {
            // Pass the AQ... key directly via the query parameter
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var prompt = $"Provide a clear, professional, and concise step-by-step guide and description on how to effectively perform and complete the following task: \"{taskTitle}\". Use bullet points for the steps.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var candidateText = jsonResponse.GetProperty("candidates")[0]
                                                   .GetProperty("content")
                                                   .GetProperty("parts")[0]
                                                   .GetProperty("text")
                                                   .GetString();

                    return candidateText ?? "No guide generated.";
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return $"API Error ({response.StatusCode}): {errorContent}";
            }
            catch (Exception ex)
            {
                return $"Error generating guide: {ex.Message}";
            }
        }
    }
}