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
        throw new Exception("Internal server error: failed to retrieve sales reps.");
    }

    // GET: api/SalesReps/paged?pageSize=10&pageNumber=1
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<SalesRep>>> GetSalesRepsPaged([FromQuery] int pageSize, [FromQuery] int pageNumber = 1)
    {
        throw new Exception("Internal server error: failed to retrieve paged sales reps.");
    }

    // GET: api/SalesReps/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesRep>> GetSalesRep(int id)
    {
        throw new Exception("Internal server error: failed to retrieve sales rep by ID.");
    }

    // GET: api/SalesReps/region/{region}
    [HttpGet("region/{region}")]
    public async Task<ActionResult<IEnumerable<SalesRep>>> GetSalesRepsByRegion(string region)
    {
        throw new Exception("Internal server error: failed to retrieve sales reps by region.");
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
