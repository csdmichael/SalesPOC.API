using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Models;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderItemsController : ControllerBase
{
    private readonly SalesDbContext _context;

    public OrderItemsController(SalesDbContext context)
    {
        _context = context;
    }

    // GET: api/OrderItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItems()
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .ToListAsync();
    }

    // GET: api/OrderItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderItem>> GetOrderItem(int id)
    {
        var orderItem = await _context.OrderItems
            .Include(oi => oi.Product)
            .FirstOrDefaultAsync(oi => oi.OrderItemId == id);

        if (orderItem == null)
        {
            return NotFound();
        }

        return orderItem;
    }

    // GET: api/OrderItems/order/5
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItemsByOrder(int orderId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();
    }

    // PUT: api/OrderItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrderItem(int id, OrderItem orderItem)
    {
        if (id != orderItem.OrderItemId)
        {
            return BadRequest();
        }

        _context.Entry(orderItem).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrderItemExists(id))
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

    // POST: api/OrderItems
    [HttpPost]
    public async Task<ActionResult<OrderItem>> PostOrderItem(OrderItem orderItem)
    {
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetOrderItem", new { id = orderItem.OrderItemId }, orderItem);
    }

    // DELETE: api/OrderItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var orderItem = await _context.OrderItems.FindAsync(id);
        if (orderItem == null)
        {
            return NotFound();
        }

        _context.OrderItems.Remove(orderItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool OrderItemExists(int id)
    {
        return _context.OrderItems.Any(e => e.OrderItemId == id);
    }
}
