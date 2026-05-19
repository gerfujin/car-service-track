using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.Vehicle;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _context;

    public VehiclesController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private async Task<Owner?> GetCurrentOwnerAsync()
    {
        var userId = GetCurrentUserId();
        return await _context.Owners
            .FirstOrDefaultAsync(o => o.AppUserId == userId);
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
        IQueryable<Vehicle> query = _context.Vehicles;

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own vehicles only
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return Ok(new List<VehicleDto>());
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicles = await query
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                LicensePlate = v.LicensePlate,
                Vin = v.Vin,
                Mileage = v.Mileage,
                Color = v.Color,
                OwnerId = v.OwnerId
            })
            .ToListAsync();

        return Ok(vehicles);
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
        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                LicensePlate = v.LicensePlate,
                Vin = v.Vin,
                Mileage = v.Mileage,
                Color = v.Color,
                OwnerId = v.OwnerId
            })
            .FirstOrDefaultAsync();

        if (vehicle == null) return NotFound();
        return Ok(vehicle);
    }

    /// <summary>
    /// Create a new vehicle.
    /// Admin: can create for any owner (uses own profile).
    /// Client: creates for themselves.
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

        // Get or create owner profile
        var owner = await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
        if (owner == null)
        {
            var user = await _context.Users.FindAsync(userId);
            owner = new Owner
            {
                AppUserId = userId,
                FirstName = user?.UserName ?? "Unknown",
                LastName = ""
            };
            _context.Owners.Add(owner);
            await _context.SaveChangesAsync();
        }

        var vehicle = new Vehicle
        {
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            LicensePlate = dto.LicensePlate,
            Vin = dto.Vin,
            Mileage = dto.Mileage,
            Color = dto.Color,
            OwnerId = owner.Id
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        var result = new VehicleDto
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            Mileage = vehicle.Mileage,
            Color = vehicle.Color,
            OwnerId = vehicle.OwnerId
        };

        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, result);
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
        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin())
        {
            // Client: IDOR check
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        vehicle.Make = dto.Make;
        vehicle.Model = dto.Model;
        vehicle.Year = dto.Year;
        vehicle.LicensePlate = dto.LicensePlate;
        vehicle.Vin = dto.Vin;
        vehicle.Mileage = dto.Mileage;
        vehicle.Color = dto.Color;
        vehicle.UpdatedAt = DateTime.UtcNow;

        _context.Entry(vehicle).State = EntityState.Modified;
        await _context.SaveChangesAsync();

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
        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        var hasOrders = await _context.ServiceOrders.AnyAsync(so => so.VehicleId == id);
        if (hasOrders)
            return BadRequest(new { message = "Cannot delete vehicle with existing service orders." });

        try
        {
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Cannot delete this vehicle. It may have related records." });
        }

        return NoContent();
    }
}
