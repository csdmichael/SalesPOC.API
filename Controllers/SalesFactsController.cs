using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Models;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalesFactsController : ControllerBase
{
    private readonly SalesDbContext _context;

    public SalesFactsController(SalesDbContext context)
    {
        _context = context;
    }

    // GET: api/SalesFacts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VwSalesFact>>> GetSalesFacts()
    {
        return await _context.VwSalesFacts.ToListAsync();
    }

    // GET: api/SalesFacts/customer/{customerName}
    [HttpGet("customer/{customerName}")]
    public async Task<ActionResult<IEnumerable<VwSalesFact>>> GetSalesFactsByCustomer(string customerName)
    {
        return await _context.VwSalesFacts
            .Where(sf => sf.CustomerName.Contains(customerName))
            .ToListAsync();
    }

    // GET: api/SalesFacts/product/{productName}
    [HttpGet("product/{productName}")]
    public async Task<ActionResult<IEnumerable<VwSalesFact>>> GetSalesFactsByProduct(string productName)
    {
        return await _context.VwSalesFacts
            .Where(sf => sf.ProductName.Contains(productName))
            .ToListAsync();
    }

    // GET: api/SalesFacts/salesrep/{repName}
    [HttpGet("salesrep/{repName}")]
    public async Task<ActionResult<IEnumerable<VwSalesFact>>> GetSalesFactsBySalesRep(string repName)
    {
        return await _context.VwSalesFacts
            .Where(sf => sf.RepName != null && sf.RepName.Contains(repName))
            .ToListAsync();
    }

    // GET: api/SalesFacts/region/{region}
    [HttpGet("region/{region}")]
    public async Task<ActionResult<IEnumerable<VwSalesFact>>> GetSalesFactsByRegion(string region)
    {
        return await _context.VwSalesFacts
            .Where(sf => sf.Region == region)
            .ToListAsync();
    }

    // GET: api/SalesFacts/category/{category}
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<VwSalesFact>>> GetSalesFactsByCategory(string category)
    {
        return await _context.VwSalesFacts
            .Where(sf => sf.ProductCategory == category)
            .ToListAsync();
    }
}
