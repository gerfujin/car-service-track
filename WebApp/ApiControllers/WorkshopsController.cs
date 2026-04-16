using App.DAL.EF;
using App.DTO.v1;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
public class WorkshopsController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkshopsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Get all workshops (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkshops()
    {
        var workshops = await _context.Workshops
            .Select(w => new
            {
                w.Id,
                Name = w.Name.ToString(),
                Address = w.Address.ToString(),
                w.Phone,
                w.Email
            })
            .ToListAsync();

        return Ok(workshops);
    }

    /// <summary>Get a specific workshop (public)</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkshop(Guid id)
    {
        var workshop = await _context.Workshops
            .Where(w => w.Id == id)
            .Select(w => new
            {
                w.Id,
                Name = w.Name.ToString(),
                Address = w.Address.ToString(),
                w.Phone,
                w.Email
            })
            .FirstOrDefaultAsync();

        if (workshop == null) return NotFound();
        return Ok(workshop);
    }
}
