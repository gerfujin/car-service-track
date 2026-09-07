using App.DTO.v1;
using App.DTO.v1.Vehicle;
using Asp.Versioning;
using Base.Helpers;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Contracts.Queries;
using Users.Application.Services;
using Users.Contracts;
using Users.Presentation.Mappers;

namespace Users.Presentation.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicles;
    private readonly IOwnerService _owners;
    private readonly ISender _sender;
    private readonly IUsersUnitOfWork _usersUow;

    public VehiclesController(IVehicleService vehicles, IOwnerService owners,
        ISender sender, IUsersUnitOfWork usersUow)
    {
        _vehicles = vehicles;
        _owners = owners;
        _sender = sender;
        _usersUow = usersUow;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    /// <summary>
    /// Get vehicles.
    /// Admin/Mechanic: all vehicles.
    /// Client: own vehicles only (IDOR).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<VehicleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles()
    {
        IEnumerable<Users.Application.DTO.BllVehicle> vehicles;

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own vehicles only (empty list if no owner profile yet).
            vehicles = await _vehicles.AllByUserAsync(GetCurrentUserId());
        }
        else
        {
            vehicles = await _vehicles.AllAsync();
        }

        var result = vehicles.Select(VehicleApiMapper.ToApiDto).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Get a specific vehicle.
    /// Admin/Mechanic: any vehicle.
    /// Client: own vehicle only (IDOR).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VehicleDto>> GetVehicle(Guid id)
    {
        Users.Application.DTO.BllVehicle? vehicle;

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — must be one of the caller's own vehicles.
            vehicle = await _vehicles.FindByUserAsync(id, GetCurrentUserId());
        }
        else
        {
            vehicle = await _vehicles.FindAsync(id);
        }

        if (vehicle == null) return NotFound();
        return Ok(VehicleApiMapper.ToApiDto(vehicle));
    }

    /// <summary>
    /// Create a new vehicle.
    /// Admin: can create for any owner (uses own profile).
    /// Client: creates for themselves. Lazily creates an Owner profile if none exists yet.
    /// Mechanic: not allowed.
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,client")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<VehicleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] VehicleCreateDto dto)
    {
        var userId = GetCurrentUserId();

        // EnsureForUserAsync: returns existing Owner, or stages a new one (no intermediate save).
        // EF inserts Owner before Vehicle in the same transaction (FK dependency ordering).
        var owner = await _owners.EnsureForUserAsync(
            userId, IdentityHelpers.GetUserEmail(User) ?? "Unknown");

        var created = _vehicles.Add(VehicleApiMapper.ToBll(dto, owner.Id));
        await _usersUow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVehicle), new { id = created.Id }, VehicleApiMapper.ToApiDto(created));
    }

    /// <summary>
    /// Update a vehicle.
    /// Admin: any vehicle.
    /// Client: own vehicle only (IDOR).
    /// Mechanic: not allowed.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,client")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] VehicleCreateDto dto)
    {
        Users.Application.DTO.BllVehicle? vehicle;

        if (!IsAdmin())
        {
            // Client: IDOR — must own the vehicle.
            vehicle = await _vehicles.FindByUserAsync(id, GetCurrentUserId());
        }
        else
        {
            vehicle = await _vehicles.FindAsync(id);
        }

        if (vehicle == null) return NotFound();

        VehicleApiMapper.ApplyUpdate(vehicle, dto);
        await _vehicles.UpdateAsync(vehicle);
        await _usersUow.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a vehicle.
    /// Admin: any vehicle.
    /// Client: own vehicle only (IDOR).
    /// Mechanic: not allowed.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,client")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        Users.Application.DTO.BllVehicle? vehicle;

        if (!IsAdmin())
        {
            vehicle = await _vehicles.FindByUserAsync(id, GetCurrentUserId());
        }
        else
        {
            vehicle = await _vehicles.FindAsync(id);
        }

        if (vehicle == null) return NotFound();

        // Cross-module check: ask the Orders module if this vehicle has any service orders.
        if (await _sender.Send(new HasServiceOrdersForVehicleQuery(id)))
            return BadRequest(new { message = "Cannot delete vehicle with existing service orders." });

        try
        {
            _vehicles.Remove(vehicle);
            await _usersUow.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Cannot delete this vehicle. It may have related records." });
        }

        return NoContent();
    }
}
