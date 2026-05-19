using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.Service;
using App.DTO.v1.ServiceOrder;
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
public class ServiceOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServiceOrdersController(AppDbContext context)
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
        return await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    /// <summary>
    /// Get service orders.
    /// Admin/Mechanic: all orders.
    /// Client: own orders only (IDOR via vehicle ownership).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<ServiceOrderDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServiceOrderDto>>> GetServiceOrders()
    {
        IQueryable<ServiceOrder> query = _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts);

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own orders only
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return Ok(new List<ServiceOrderDto>());
            query = query.Where(so => so.Vehicle!.OwnerId == owner.Id);
        }

        var orders = await query
            .Select(so => new ServiceOrderDto
            {
                Id = so.Id,
                Description = so.Description,
                Status = so.Status,
                OrderDate = so.OrderDate,
                CompletedDate = so.CompletedDate,
                VehicleId = so.VehicleId,
                VehicleDisplay = so.Vehicle != null ? $"{so.Vehicle.Make} {so.Vehicle.Model} ({so.Vehicle.LicensePlate})" : null,
                WorkshopId = so.WorkshopId,
                WorkshopName = so.Workshop != null ? so.Workshop.Name.ToString() : null,
                MechanicId = so.MechanicId,
                MechanicName = so.Mechanic != null ? $"{so.Mechanic.FirstName} {so.Mechanic.LastName}" : null,
                TotalAmount = (so.ServiceOrderItems != null
                    ? so.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice)
                    : 0) +
                    (so.ServiceOrderParts != null
                    ? so.ServiceOrderParts.Sum(p => p.Quantity * p.UnitPrice)
                    : 0),
                FinalPrice = so.FinalPrice
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Get a specific service order.
    /// Admin/Mechanic: any order.
    /// Client: own order only (IDOR).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<ServiceOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceOrderDto>> GetServiceOrder(Guid id)
    {
        IQueryable<ServiceOrder> query = _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems!)
                .ThenInclude(item => item.Service)
            .Include(so => so.ServiceOrderParts)
            .Where(so => so.Id == id);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(so => so.Vehicle!.OwnerId == owner.Id);
        }

        var order = await query
            .Select(so => new ServiceOrderDto
            {
                Id = so.Id,
                Description = so.Description,
                Status = so.Status,
                OrderDate = so.OrderDate,
                CompletedDate = so.CompletedDate,
                VehicleId = so.VehicleId,
                VehicleDisplay = so.Vehicle != null ? $"{so.Vehicle.Make} {so.Vehicle.Model} ({so.Vehicle.LicensePlate})" : null,
                WorkshopId = so.WorkshopId,
                WorkshopName = so.Workshop != null ? so.Workshop.Name.ToString() : null,
                MechanicId = so.MechanicId,
                MechanicName = so.Mechanic != null ? $"{so.Mechanic.FirstName} {so.Mechanic.LastName}" : null,
                TotalAmount = (so.ServiceOrderItems != null
                    ? so.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice)
                    : 0) +
                    (so.ServiceOrderParts != null
                    ? so.ServiceOrderParts.Sum(p => p.Quantity * p.UnitPrice)
                    : 0),
                FinalPrice = so.FinalPrice,
                Services = so.ServiceOrderItems != null
                    ? so.ServiceOrderItems
                        .Where(item => item.Service != null)
                        .Select(item => new ServiceDto
                        {
                            Id = item.Service!.Id,
                            Name = item.Service!.Name.ToString() ?? string.Empty,
                            Description = item.Service!.Description != null ? item.Service!.Description.ToString() : null,
                            BasePrice = item.Service!.BasePrice,
                            EstimatedTimeMinutes = item.Service!.EstimatedTimeMinutes
                        }).ToList()
                    : new List<ServiceDto>()
            })
            .FirstOrDefaultAsync();

        if (order == null) return NotFound();
        return Ok(order);
    }

    /// <summary>
    /// Create a new service order.
    /// Admin: can create for any vehicle.
    /// Client: can create only for own vehicle (IDOR).
    /// Mechanic: not allowed.
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,client")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<ServiceOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceOrderDto>> CreateServiceOrder([FromBody] ServiceOrderCreateDto dto)
    {
        Vehicle? vehicle;

        if (IsAdmin())
        {
            // Admin can create for any vehicle
            vehicle = await _context.Vehicles.FindAsync(dto.VehicleId);
            if (vehicle == null)
            {
                return BadRequest(new RestApiErrorResponse
                {
                    Status = System.Net.HttpStatusCode.BadRequest,
                    Error = "Vehicle not found."
                });
            }
        }
        else
        {
            // Client: IDOR — vehicle must belong to current user
            var owner = await GetCurrentOwnerAsync();
            if (owner == null)
            {
                return BadRequest(new RestApiErrorResponse
                {
                    Status = System.Net.HttpStatusCode.BadRequest,
                    Error = "Owner profile not found. Please create a vehicle first."
                });
            }

            vehicle = await _context.Vehicles
                .Where(v => v.Id == dto.VehicleId && v.OwnerId == owner.Id)
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return BadRequest(new RestApiErrorResponse
                {
                    Status = System.Net.HttpStatusCode.BadRequest,
                    Error = "Vehicle not found or does not belong to you."
                });
            }
        }

        var workshop = await _context.Workshops.FindAsync(dto.WorkshopId);
        if (workshop == null)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Workshop not found."
            });
        }

        var order = new ServiceOrder
        {
            Description = dto.Description,
            VehicleId = dto.VehicleId,
            WorkshopId = dto.WorkshopId,
            Status = ServiceOrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };

        _context.ServiceOrders.Add(order);

        // Add selected services as ServiceOrderItems
        if (dto.ServiceIds != null && dto.ServiceIds.Any())
        {
            var services = await _context.Services
                .Where(s => dto.ServiceIds.Contains(s.Id))
                .ToListAsync();

            foreach (var service in services)
            {
                var serviceItem = new ServiceOrderItem
                {
                    ServiceOrderId = order.Id,
                    ServiceId = service.Id,
                    Quantity = 1,
                    UnitPrice = service.BasePrice
                };
                _context.ServiceOrderItems.Add(serviceItem);
            }
        }

        var statusHistory = new ServiceOrderStatusHistory
        {
            ServiceOrderId = order.Id,
            Status = ServiceOrderStatus.Pending,
            Notes = "Order created",
            ChangedAt = DateTime.UtcNow
        };
        _context.ServiceOrderStatusHistories.Add(statusHistory);

        await _context.SaveChangesAsync();

        var result = new ServiceOrderDto
        {
            Id = order.Id,
            Description = order.Description,
            Status = order.Status,
            OrderDate = order.OrderDate,
            VehicleId = order.VehicleId,
            VehicleDisplay = $"{vehicle.Make} {vehicle.Model} ({vehicle.LicensePlate})",
            WorkshopId = order.WorkshopId,
            WorkshopName = workshop.Name.ToString(),
            TotalAmount = 0
        };

        return CreatedAtAction(nameof(GetServiceOrder), new { id = order.Id }, result);
    }

    /// <summary>
    /// Update service order status.
    /// Admin: any order.
    /// Mechanic: any order (status workflow).
    /// Client: not allowed.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null) return NotFound();

        var previousStatus = order.Status;
        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.Status == ServiceOrderStatus.Completed)
        {
            order.CompletedDate = DateTime.UtcNow;
        }

        var statusHistory = new ServiceOrderStatusHistory
        {
            ServiceOrderId = order.Id,
            Status = dto.Status,
            Notes = dto.Notes ?? $"Status changed from {previousStatus} to {dto.Status}",
            ChangedAt = DateTime.UtcNow
        };
        _context.ServiceOrderStatusHistories.Add(statusHistory);

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get status history for a service order.
    /// Admin/Mechanic: any order.
    /// Client: own order only (IDOR).
    /// </summary>
    [HttpGet("{id:guid}/status-history")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatusHistory(Guid id)
    {
        // Verify access
        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();

            var orderExists = await _context.ServiceOrders
                .AnyAsync(so => so.Id == id && so.Vehicle!.OwnerId == owner.Id);
            if (!orderExists) return NotFound();
        }
        else
        {
            var orderExists = await _context.ServiceOrders.AnyAsync(so => so.Id == id);
            if (!orderExists) return NotFound();
        }

        var history = await _context.ServiceOrderStatusHistories
            .Where(h => h.ServiceOrderId == id)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new
            {
                h.Id,
                Status = h.Status.ToString(),
                h.Notes,
                h.ChangedAt
            })
            .ToListAsync();

        return Ok(history);
    }
}
