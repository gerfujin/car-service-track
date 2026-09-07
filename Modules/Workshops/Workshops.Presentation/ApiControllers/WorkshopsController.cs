using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Workshops.Application.Services;
using Workshops.Presentation.Mappers;

namespace Workshops.Presentation.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
public class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshops;

    public WorkshopsController(IWorkshopService workshops)
    {
        _workshops = workshops;
    }

    /// <summary>Get all workshops (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkshops()
    {
        var workshops = (await _workshops.AllAsync())
            .Select(WorkshopApiMapper.ToApiDto)
            .ToList();

        return Ok(workshops);
    }

    /// <summary>Get a specific workshop (public)</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkshop(Guid id)
    {
        var workshop = await _workshops.FindAsync(id);
        if (workshop == null) return NotFound();
        return Ok(WorkshopApiMapper.ToApiDto(workshop));
    }
}
