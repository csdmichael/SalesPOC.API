using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Models;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SalesRepsController : ControllerBase
{
    private readonly SalesDbContext _context;

    public SalesRepsController(SalesDbContext context)
    {
        _context = context;
    }

    // GET: api/SalesReps
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesRep>>> GetSalesReps()
    {
        return await _context.SalesReps.ToListAsync();
    }

    // GET: api/SalesReps/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesRep>> GetSalesRep(int id)
    {
        var salesRep = await _context.SalesReps.FindAsync(id);

        if (salesRep == null)
        {
            return NotFound();
        }

        return salesRep;
    }

    // GET: api/SalesReps/region/{region}
    [HttpGet("region/{region}")]
    public async Task<ActionResult<IEnumerable<SalesRep>>> GetSalesRepsByRegion(string region)
    {
        return await _context.SalesReps
            .Where(r => r.Region == region)
            .ToListAsync();
    }

    // PUT: api/SalesReps/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSalesRep(int id, SalesRep salesRep)
    {
        if (id != salesRep.SalesRepId)
        {
            return BadRequest();
        }

        _context.Entry(salesRep).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SalesRepExists(id))
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

    // POST: api/SalesReps
    [HttpPost]
    public async Task<ActionResult<SalesRep>> PostSalesRep(SalesRep salesRep)
    {
        _context.SalesReps.Add(salesRep);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSalesRep", new { id = salesRep.SalesRepId }, salesRep);
    }

    // DELETE: api/SalesReps/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSalesRep(int id)
    {
        var salesRep = await _context.SalesReps.FindAsync(id);
        if (salesRep == null)
        {
            return NotFound();
        }

        _context.SalesReps.Remove(salesRep);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SalesRepExists(int id)
    {
        return _context.SalesReps.Any(e => e.SalesRepId == id);
    }
}
