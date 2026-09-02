using App.DTO.v1.Mechanic;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Workshops.Application.Services;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
public class MechanicsController : ControllerBase
{
    private readonly IMechanicService _mechanics;

    public MechanicsController(IMechanicService mechanics)
    {
        _mechanics = mechanics;
    }

    /// <summary>Get all mechanics (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MechanicDto>>> GetMechanics()
    {
        var mechanics = (await _mechanics.AllAsync())
            .Select(MechanicApiMapper.ToApiDto)
            .ToList();

        return Ok(mechanics);
    }

    /// <summary>Get a specific mechanic (public)</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MechanicDto>> GetMechanic(Guid id)
    {
        var mechanic = await _mechanics.FindAsync(id);

        if (mechanic == null) return NotFound();
        return Ok(MechanicApiMapper.ToApiDto(mechanic));
    }
}
