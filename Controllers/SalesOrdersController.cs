using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Models;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalesOrdersController : ControllerBase
{
    private readonly SalesDbContext _context;

    public SalesOrdersController(SalesDbContext context)
    {
        _context = context;
    }

    // GET: api/SalesOrders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrders()
    {
        return await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.SalesRep)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();
    }

    // GET: api/SalesOrders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesOrder>> GetSalesOrder(int id)
    {
        var salesOrder = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.SalesRep)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (salesOrder == null)
        {
            return NotFound();
        }

        return salesOrder;
    }

    // GET: api/SalesOrders/customer/5
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrdersByCustomer(int customerId)
    {
        return await _context.SalesOrders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();
    }

    // GET: api/SalesOrders/salesrep/5
    [HttpGet("salesrep/{salesRepId}")]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrdersBySalesRep(int salesRepId)
    {
        return await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.SalesRepId == salesRepId)
            .ToListAsync();
    }

    // PUT: api/SalesOrders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSalesOrder(int id, SalesOrder salesOrder)
    {
        if (id != salesOrder.OrderId)
        {
            return BadRequest();
        }

        _context.Entry(salesOrder).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SalesOrderExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/SalesOrders
    [HttpPost]
    public async Task<ActionResult<SalesOrder>> PostSalesOrder(SalesOrder salesOrder)
    {
        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSalesOrder", new { id = salesOrder.OrderId }, salesOrder);
    }

    // DELETE: api/SalesOrders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSalesOrder(int id)
    {
        var salesOrder = await _context.SalesOrders.FindAsync(id);
        if (salesOrder == null)
        {
            return NotFound();
        }

        _context.SalesOrders.Remove(salesOrder);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SalesOrderExists(int id)
    {
        return _context.SalesOrders.Any(e => e.OrderId == id);
    }
}
