using App.BLL;
using App.DTO.v1;
using App.DTO.v1.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ServicesController : ControllerBase
{
    private readonly IAppBll _bll;

    public ServicesController(IAppBll bll)
    {
        _bll = bll;
    }

    /// <summary>Get all services (public)</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<ServiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> GetServices()
    {
        var services = (await _bll.Services.AllAsync())
            .Select(ServiceApiMapper.ToApiDto)
            .ToList();

        return Ok(services);
    }

    /// <summary>Get a specific service (public)</summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetService(Guid id)
    {
        var service = await _bll.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.NotFound,
                Error = "Service not found."
            });
        }

        return Ok(ServiceApiMapper.ToApiDto(service));
    }

    /// <summary>Create a service (admin only)</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] ServiceCreateDto dto)
    {
        var validationError = ValidateServiceInput(dto.Name, dto.BasePrice, dto.EstimatedTimeMinutes);
        if (validationError != null) return validationError;

        var created = _bll.Services.Add(ServiceApiMapper.ToBll(dto));
        await _bll.SaveChangesAsync();

        return CreatedAtAction(nameof(GetService), new { id = created.Id }, ServiceApiMapper.ToApiDto(created));
    }

    /// <summary>Update a service (admin only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] ServiceUpdateDto dto)
    {
        var validationError = ValidateServiceInput(dto.Name, dto.BasePrice, dto.EstimatedTimeMinutes);
        if (validationError != null) return validationError;

        var updated = await _bll.Services.UpdateAsync(ServiceApiMapper.ToBll(dto, id));
        if (updated == null)
        {
            return NotFound(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.NotFound,
                Error = "Service not found."
            });
        }

        await _bll.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete a service (admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var service = await _bll.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.NotFound,
                Error = "Service not found."
            });
        }

        _bll.Services.Remove(service);
        await _bll.SaveChangesAsync();

        return NoContent();
    }

    private ActionResult? ValidateServiceInput(string name, decimal basePrice, int estimatedTimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Name is required."
            });
        }

        if (basePrice < 0)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "BasePrice must be greater than or equal to 0."
            });
        }

        if (estimatedTimeMinutes < 0)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "EstimatedTimeMinutes must be greater than or equal to 0."
            });
        }

        return null;
    }
}
