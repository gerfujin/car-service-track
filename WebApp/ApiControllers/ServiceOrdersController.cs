using App.DTO.v1;
using App.DTO.v1.ServiceOrder;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Domain.Enums;
using Users.Application.Services;
using Workshops.Application.Services;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ServiceOrdersController : ControllerBase
{
    private readonly IServiceOrderService _serviceOrders;
    private readonly IStatusHistoryService _statusHistories;
    private readonly IVehicleService _vehicles;
    private readonly IOwnerService _owners;
    private readonly IWorkshopService _workshops;
    private readonly IOrdersUnitOfWork _ordersUow;

    public ServiceOrdersController(
        IServiceOrderService serviceOrders,
        IStatusHistoryService statusHistories,
        IVehicleService vehicles,
        IOwnerService owners,
        IWorkshopService workshops,
        IOrdersUnitOfWork ordersUow)
    {
        _serviceOrders = serviceOrders;
        _statusHistories = statusHistories;
        _vehicles = vehicles;
        _owners = owners;
        _workshops = workshops;
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
        IEnumerable<BllServiceOrder> orders;

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own orders only (empty if no owner profile). Owner -> AppUser
            // filtering lives in the repository query.
            orders = await _serviceOrders.AllByUserAsync(GetCurrentUserId());
        }
        else
        {
            orders = await _serviceOrders.AllWithDetailsAsync();
        }

        var result = orders.Select(ServiceOrderApiMapper.ToApiSummaryDto).ToList();
        return Ok(result);
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
        var order = await _serviceOrders.FindWithDetailsAsync(id);
        if (order == null) return NotFound();

        if (!IsAdmin() && !IsMechanic() && order.OwnerAppUserId != GetCurrentUserId())
        {
            // Client: IDOR — not their order.
            return NotFound();
        }

        return Ok(ServiceOrderApiMapper.ToApiDto(order));
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
        var userId = GetCurrentUserId();

        Users.Application.DTO.BllVehicle? vehicle;
        if (IsAdmin())
        {
            // Admin can create for any vehicle.
            vehicle = await _vehicles.FindAsync(dto.VehicleId);
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
            // Client: IDOR. Keep the two distinct error messages byte-for-byte.
            var owner = await _owners.FindByUserAsync(userId);
            if (owner == null)
            {
                return BadRequest(new RestApiErrorResponse
                {
                    Status = System.Net.HttpStatusCode.BadRequest,
                    Error = "Owner profile not found. Please create a vehicle first."
                });
            }

            vehicle = (await _vehicles.AllByUserAsync(userId))
                .FirstOrDefault(v => v.Id == dto.VehicleId);
            if (vehicle == null)
            {
                return BadRequest(new RestApiErrorResponse
                {
                    Status = System.Net.HttpStatusCode.BadRequest,
                    Error = "Vehicle not found or does not belong to you."
                });
            }
        }

        // Workshop existence validation.
        var workshop = await _workshops.FindAsync(dto.WorkshopId);
        if (workshop == null)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Workshop not found."
            });
        }

        var orderDate = DateTime.UtcNow;

        // AddWithItemsAsync stages the ServiceOrder and then looks up each Service by id
        // to stage properly-priced ServiceOrderItems (Quantity=1, UnitPrice=Service.BasePrice).
        var created = await _serviceOrders.AddWithItemsAsync(new BllServiceOrder
        {
            Description = dto.Description,
            VehicleId = dto.VehicleId,
            WorkshopId = dto.WorkshopId,
            Status = ServiceOrderStatus.Pending,
            OrderDate = orderDate,
            ServiceIds = dto.ServiceIds ?? new List<Guid>()
        });
        var orderId = created.Id;

        _statusHistories.Add(new BllStatusHistory
        {
            ServiceOrderId = orderId,
            Status = ServiceOrderStatus.Pending,
            Notes = "Order created",
            ChangedAt = orderDate
        });

        await _ordersUow.SaveChangesAsync();

        // Minimal response — byte-for-byte with the old create (TotalAmount 0, no Services).
        var result = new ServiceOrderDto
        {
            Id = orderId,
            Description = dto.Description,
            Status = ServiceOrderStatus.Pending,
            OrderDate = orderDate,
            VehicleId = dto.VehicleId,
            VehicleDisplay = $"{vehicle.Make} {vehicle.Model} ({vehicle.LicensePlate})",
            WorkshopId = dto.WorkshopId,
            WorkshopName = workshop.Name,
            TotalAmount = 0
        };

        return CreatedAtAction(nameof(GetServiceOrder), new { id = orderId }, result);
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
        if (!await _serviceOrders.SetStatusAsync(id, dto.Status, dto.Notes)) return NotFound();
        await _ordersUow.SaveChangesAsync();
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
        if (!IsAdmin() && !IsMechanic())
        {
            if (!await _serviceOrders.IsOwnedByUserAsync(id, GetCurrentUserId())) return NotFound();
        }
        else
        {
            if (!await _serviceOrders.ExistsAsync(id)) return NotFound();
        }

        var history = (await _statusHistories.AllByOrderAsync(id))
            .Select(h => new
            {
                h.Id,
                Status = h.Status.ToString(),
                h.Notes,
                h.ChangedAt
            })
            .ToList();

        return Ok(history);
    }
}
