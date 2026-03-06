using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private const int MaxQuestionLength = 4000;

    private readonly AIProjectClient _projectClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AIProjectClient projectClient, IConfiguration configuration, ILogger<ChatController> logger)
    {
        _projectClient = projectClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sends a natural language question to the Azure AI Foundry agent and returns its response.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostQuestion([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        if (request.Question.Length > MaxQuestionLength)
        {
            return BadRequest(new ChatResponse
            {
                Reply = $"Question is too long. Please keep it under {MaxQuestionLength} characters.",
                Citations = null
            });
        }

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

            // Extract citations from response annotations
            var citations = ExtractCitations(response);

            return Ok(new ChatResponse
            {
                Reply = response.GetOutputText(),
                Citations = citations.Count > 0 ? citations : null
            });
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Azure authentication failed. Run 'az login' to refresh credentials.");
            return StatusCode(503, new ChatResponse
            {
                Reply = "Sorry, I could not reach the AI assistant right now. Please sign in to Azure (az login) and try again.",
                Citations = null
            });
        }
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            _logger.LogError(ex, "Access denied to AI project. Status={Status}", ex.Status);
            return StatusCode(503, new ChatResponse
            {
                Reply = "Sorry, I could not reach the AI assistant right now. Access to the AI project was denied.",
                Citations = null
            });
        }
        catch (RequestFailedException ex) when (ex.Status == 400 &&
            ex.Message.Contains("context_length_exceeded", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Context length exceeded. Status={Status}", ex.Status);
            return BadRequest(new ChatResponse
            {
                Reply = "Your request is too large for the AI model context window. Please shorten your prompt and try again.",
                Citations = null
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure AI request failed. Status={Status}, ErrorCode={ErrorCode}, Message={Message}",
                ex.Status, ex.ErrorCode, ex.Message);
            return StatusCode(503, new ChatResponse
            {
                Reply = $"Sorry, I could not reach the AI assistant right now (service status: {ex.Status}).",
                Citations = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error communicating with AI agent.");
            return StatusCode(500, new ChatResponse
            {
                Reply = $"Error communicating with the AI agent: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Extracts citation information from the agent response output items.
    /// Supports file citations, URL citations, and container file citations.
    /// </summary>
    private static List<Citation> ExtractCitations(ResponseResult response)
    {
        var citations = new List<Citation>();

        foreach (var item in response.OutputItems)
        {
            if (item is not MessageResponseItem message)
                continue;

            foreach (var part in message.Content)
            {
                var annotations = part.OutputTextAnnotations;
                if (annotations is null)
                    continue;

                foreach (var annotation in annotations)
                {
                    switch (annotation)
                    {
                        case FileCitationMessageAnnotation fileCitation:
                            citations.Add(new Citation
                            {
                                Type = "file",
                                Title = fileCitation.Filename,
                                FileId = fileCitation.FileId
                            });
                            break;

                        case UriCitationMessageAnnotation uriCitation:
                            citations.Add(new Citation
                            {
                                Type = "url",
                                Title = uriCitation.Title,
                                Url = uriCitation.Uri?.ToString()
                            });
                            break;

                        case ContainerFileCitationMessageAnnotation containerCitation:
                            citations.Add(new Citation
                            {
                                Type = "container_file",
                                Title = containerCitation.Filename,
                                FileId = containerCitation.FileId
                            });
                            break;
                    }
                }
            }
        }

        // Deduplicate by title + fileId/url
        return citations
            .GroupBy(c => $"{c.Type}|{c.Title}|{c.FileId}|{c.Url}")
            .Select(g => g.First())
            .ToList();
    }
}

public class ChatRequest
{
    public string Question { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public List<Citation>? Citations { get; set; }
}

public class Citation
{
    public string Type { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? FileId { get; set; }
}
