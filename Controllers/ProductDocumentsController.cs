using Microsoft.AspNetCore.Mvc;
using SalesAPI.Services;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductDocumentsController : ControllerBase
{
    private readonly BlobStorageService _blobService;

    public ProductDocumentsController(BlobStorageService blobService)
    {
        _blobService = blobService;
    }

    /// <summary>
    /// GET: api/ProductDocuments
    /// Lists all documents across all products in the blob container.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllDocuments()
    {
        var documents = await _blobService.GetAllDocumentsAsync();
        return Ok(documents);
    }

    /// <summary>
    /// GET: api/ProductDocuments/products
    /// Returns a list of distinct product names found in the container.
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProductNames()
    {
        var products = await _blobService.GetProductNamesAsync();
        return Ok(products);
    }

    /// <summary>
    /// GET: api/ProductDocuments/by-product/{productName}
    /// Lists all documents for a specific product (e.g. "Chip-1").
    /// Each product typically has: Engineering DataSheet, Marketing Brief, Sales One Pager.
    /// </summary>
    [HttpGet("by-product/{productName}")]
    public async Task<IActionResult> GetDocumentsByProduct(string productName)
    {
        var documents = await _blobService.GetDocumentsByProductAsync(productName);

        if (documents.Count == 0)
            return NotFound(new { message = $"No documents found for product '{productName}'." });

        return Ok(documents);
    }

    /// <summary>
    /// GET: api/ProductDocuments/download?blobName=Chip-1/Engineering_DataSheet.pdf
    /// Downloads a specific document by its full blob name.
    /// </summary>
    [HttpGet("download")]
    public async Task<IActionResult> DownloadDocument([FromQuery] string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return BadRequest(new { message = "blobName query parameter is required." });

        var result = await _blobService.DownloadDocumentAsync(blobName);

        if (result is null)
            return NotFound(new { message = $"Blob '{blobName}' not found." });

        var (content, contentType, fileName) = result.Value;
        return File(content, contentType, fileName);
    }
}
