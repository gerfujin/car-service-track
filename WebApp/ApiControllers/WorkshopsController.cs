using App.BLL;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
public class WorkshopsController : ControllerBase
{
    private readonly IAppBll _bll;

    public WorkshopsController(IAppBll bll)
    {
        _bll = bll;
    }

    /// <summary>Get all workshops (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkshops()
    {
        var workshops = (await _bll.Workshops.AllAsync())
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
        var workshop = await _bll.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();
        return Ok(WorkshopApiMapper.ToApiDto(workshop));
    }
}
