using Microsoft.AspNetCore.Mvc;
using SalesAPI.Models;
using SalesAPI.Services;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductDescriptionsController : ControllerBase
{
    private readonly CosmosDbService _cosmosService;

    public ProductDescriptionsController(CosmosDbService cosmosService)
    {
        _cosmosService = cosmosService;
    }

    /// <summary>
    /// GET: api/ProductDescriptions
    /// Retrieves all product descriptions from CosmosDB.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var descriptions = await _cosmosService.GetAllProductDescriptionsAsync();
        return Ok(descriptions);
    }

    /// <summary>
    /// GET: api/ProductDescriptions/paged?pageSize=10&pageNumber=1
    /// Retrieves paged product descriptions from CosmosDB.
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<ProductDescription>>> GetAllPaged([FromQuery] int pageSize, [FromQuery] int pageNumber = 1)
    {
        if (pageSize <= 0)
            return BadRequest(new { message = "pageSize must be greater than 0." });

        if (pageNumber <= 0)
            return BadRequest(new { message = "pageNumber must be greater than 0." });

        var descriptions = await _cosmosService.GetAllProductDescriptionsAsync();
        var totalRecords = descriptions.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var items = descriptions
            .OrderBy(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResponse<ProductDescription>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages
        });
    }

    /// <summary>
    /// GET: api/ProductDescriptions/{productId}
    /// Retrieves a product description by its ID (e.g. "Chip-100").
    /// </summary>
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetById(string productId)
    {
        var description = await _cosmosService.GetProductDescriptionByIdAsync(productId);

        if (description is null)
            return NotFound(new { message = $"No product description found for '{productId}'." });

        return Ok(description);
    }

    /// <summary>
    /// GET: api/ProductDescriptions/search?q=chip
    /// Searches product descriptions by partial ID match.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required." });

        var results = await _cosmosService.SearchProductDescriptionsAsync(q);
        return Ok(results);
    }
}
