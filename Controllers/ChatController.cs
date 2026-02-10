using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly AIProjectClient _projectClient;
    private readonly IConfiguration _configuration;

    public ChatController(AIProjectClient projectClient, IConfiguration configuration)
    {
        _projectClient = projectClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Sends a natural language question to the Azure AI Foundry agent and returns its response.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostQuestion([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        try
        {
            var agentName = _configuration["AzureAgent:AgentName"]
                ?? throw new InvalidOperationException("AzureAgent:AgentName is not configured.");

            // Retrieve the agent by name
            AgentRecord agentRecord = _projectClient.Agents.GetAgent(agentName);

            // Get a Responses API client scoped to this agent
            ProjectResponsesClient responseClient =
                _projectClient.OpenAI.GetProjectResponsesClientForAgent(agentRecord);

            // Call the agent (runs async on the thread pool so the request stays non-blocking)
            ResponseResult response = await Task.Run(
                () => responseClient.CreateResponse(request.Question));

            return Ok(new ChatResponse { Reply = response.GetOutputText() });
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
