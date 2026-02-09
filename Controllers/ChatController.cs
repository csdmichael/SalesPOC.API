using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Proxies a natural language question to the Azure AI Foundry agent.
    /// Configure "AzureAgent:Endpoint" and "AzureAgent:ApiKey" in appsettings.json.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostQuestion([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        var endpoint = _configuration["AzureAgent:Endpoint"];
        var apiKey = _configuration["AzureAgent:ApiKey"];

        if (string.IsNullOrEmpty(endpoint))
        {
            // Fallback: return a mock response if the agent is not configured
            return Ok(new ChatResponse
            {
                Reply = $"[Agent not configured] You asked: \"{request.Question}\". " +
                        "Please configure AzureAgent:Endpoint and AzureAgent:ApiKey in appsettings.json " +
                        "to connect to the Arrow Sales Agent on Azure AI Foundry."
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("api-key", apiKey);
            }

            var payload = new
            {
                messages = new[]
                {
                    new { role = "user", content = request.Question }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse the agent response - adapt based on actual response format
                using var doc = JsonDocument.Parse(responseBody);
                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Ok(new ChatResponse { Reply = reply ?? "No response from agent." });
            }

            return StatusCode((int)response.StatusCode, new ChatResponse
            {
                Reply = $"Agent returned error: {response.StatusCode}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ChatResponse
            {
                Reply = $"Error communicating with the AI agent: {ex.Message}"
            });
        }
    }
}

public class ChatRequest
{
    public string Question { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
}
