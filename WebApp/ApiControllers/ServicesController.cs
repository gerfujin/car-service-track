using App.DAL.EF;
using App.Domain;
using App.DTO.v1.Service;
using Asp.Versioning;
using Base.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Get all services (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<ServiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices()
    {
        var services = await _context.Services
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name.ToString(),
                Description = s.Description.ToString(),
                BasePrice = s.BasePrice
            })
            .ToListAsync();

        return Ok(services);
    }

    /// <summary>Get a specific service (public)</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetService(Guid id)
    {
        var service = await _context.Services
            .Where(s => s.Id == id)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name.ToString(),
                Description = s.Description.ToString(),
                BasePrice = s.BasePrice
            })
            .FirstOrDefaultAsync();

        if (service == null) return NotFound();
        return Ok(service);
    }

    /// <summary>Create a service (admin only)</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] ServiceDto dto)
    {
        var service = new Service
        {
            Name = new LangStr(dto.Name),
            Description = new LangStr(dto.Description),
            BasePrice = dto.BasePrice
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        dto.Id = service.Id;
        return CreatedAtAction(nameof(GetService), new { id = service.Id }, dto);
    }

    /// <summary>Update a service (admin only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] ServiceDto dto)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        service.Name = new LangStr(dto.Name);
        service.Description = new LangStr(dto.Description);
        service.BasePrice = dto.BasePrice;
        service.UpdatedAt = DateTime.UtcNow;

        _context.Entry(service).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete a service (admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
