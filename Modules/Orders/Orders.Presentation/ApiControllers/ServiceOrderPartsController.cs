using App.DTO.v1;
using App.DTO.v1.ServiceOrderPart;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;
using Orders.Contracts;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ServiceOrderPartsController : ControllerBase
{
    private readonly IServiceOrderPartService _serviceOrderParts;
    private readonly IOrdersUnitOfWork _ordersUow;

    public ServiceOrderPartsController(IServiceOrderPartService serviceOrderParts, IOrdersUnitOfWork ordersUow)
    {
        _serviceOrderParts = serviceOrderParts;
        _ordersUow = ordersUow;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    private static RestApiErrorResponse ErrorResponse(string error) => new()
    {
        Status = System.Net.HttpStatusCode.BadRequest,
        Error = error
    };

    [HttpGet]
    [ProducesResponseType<IEnumerable<ServiceOrderPartDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServiceOrderPartDto>>> GetServiceOrderParts([FromQuery] Guid? serviceOrderId)
    {
        var isAdmin = IsAdmin();
        var isMechanic = IsMechanic();
        var appUserId = isAdmin || isMechanic ? Guid.Empty : GetCurrentUserId();

        var parts = (await _serviceOrderParts.AllForApiAsync(serviceOrderId, appUserId, isAdmin, isMechanic))
            .Select(ServiceOrderPartApiMapper.ToApiDto)
            .ToList();

        return Ok(parts);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServiceOrderPartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceOrderPartDto>> GetServiceOrderPart(Guid id)
    {
        var isAdmin = IsAdmin();
        var isMechanic = IsMechanic();
        var appUserId = isAdmin || isMechanic ? Guid.Empty : GetCurrentUserId();

        var sop = await _serviceOrderParts.FindForApiAsync(id, appUserId, isAdmin, isMechanic);

        if (sop == null) return NotFound();
        return Ok(ServiceOrderPartApiMapper.ToApiDto(sop));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType<ServiceOrderPartDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceOrderPartDto>> CreateServiceOrderPart([FromBody] ServiceOrderPartCreateDto dto)
    {
        var result = await _serviceOrderParts.CreateForApiAsync(ServiceOrderPartApiMapper.ToBll(dto));
        if (result.Error != null)
        {
            return BadRequest(ErrorResponse(result.Error));
        }

        await _ordersUow.SaveChangesAsync();
        if (result.RecalculateServiceOrderId.HasValue)
        {
            await _serviceOrderParts.RecalculateOrderTotalAsync(result.RecalculateServiceOrderId.Value);
            await _ordersUow.SaveChangesAsync();
        }

        return CreatedAtAction(
            nameof(GetServiceOrderPart),
            new { id = result.Entity!.Id },
            ServiceOrderPartApiMapper.ToApiDto(result.Entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateServiceOrderPart(Guid id, [FromBody] ServiceOrderPartUpdateDto dto)
    {
        var result = await _serviceOrderParts.UpdateForApiAsync(id, ServiceOrderPartApiMapper.ToBll(dto, id));
        if (result.NotFound) return NotFound();
        if (result.Error != null)
        {
            return BadRequest(ErrorResponse(result.Error));
        }

        await _ordersUow.SaveChangesAsync();
        if (result.RecalculateServiceOrderId.HasValue)
        {
            await _serviceOrderParts.RecalculateOrderTotalAsync(result.RecalculateServiceOrderId.Value);
            await _ordersUow.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServiceOrderPart(Guid id)
    {
        var result = await _serviceOrderParts.RemoveForApiAsync(id);
        if (result.NotFound) return NotFound();

        await _ordersUow.SaveChangesAsync();
        if (result.RecalculateServiceOrderId.HasValue)
        {
            await _serviceOrderParts.RecalculateOrderTotalAsync(result.RecalculateServiceOrderId.Value);
            await _ordersUow.SaveChangesAsync();
        }

        return NoContent();
    }
}
