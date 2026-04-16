using App.DAL.EF;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
public class MechanicsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MechanicsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Get all mechanics (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMechanics()
    {
        var mechanics = await _context.Mechanics
            .Select(m => new
            {
                m.Id,
                m.FirstName,
                m.LastName,
                FullName = m.FirstName + " " + m.LastName,
                m.Phone,
                m.Email,
                m.Specialization
            })
            .ToListAsync();

        return Ok(mechanics);
    }

    /// <summary>Get a specific mechanic (public)</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMechanic(Guid id)
    {
        var mechanic = await _context.Mechanics
            .Where(m => m.Id == id)
            .Select(m => new
            {
                m.Id,
                m.FirstName,
                m.LastName,
                FullName = m.FirstName + " " + m.LastName,
                m.Phone,
                m.Email,
                m.Specialization
            })
            .FirstOrDefaultAsync();

        if (mechanic == null) return NotFound();
        return Ok(mechanic);
    }
}
