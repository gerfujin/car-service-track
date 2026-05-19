using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.Payment;
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
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
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

    private static PaymentDto MapToDto(Payment p)
    {
        return new PaymentDto
        {
            Id = p.Id,
            Amount = p.Amount,
            Status = p.Status,
            PaidAt = p.PaidAt,
            PaymentMethod = p.PaymentMethod,
            Notes = p.Notes,
            ServiceOrderId = p.ServiceOrderId,
            OwnerId = p.ServiceOrder?.Vehicle?.OwnerId,
            CreatedAt = p.CreatedAt,
            VehicleInfo = p.ServiceOrder?.Vehicle != null
                ? $"{p.ServiceOrder.Vehicle.Make} {p.ServiceOrder.Vehicle.Model} ({p.ServiceOrder.Vehicle.LicensePlate})"
                : null,
            WorkshopName = p.ServiceOrder?.Workshop?.Name.ToString()
        };
    }

    /// <summary>
    /// Get payments.
    /// Admin/Mechanic: all payments.
    /// Client: own payments only (IDOR via vehicle ownership).
    /// Optional filter: ?serviceOrderId=guid
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<PaymentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments([FromQuery] Guid? serviceOrderId = null)
    {
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Workshop);

        if (serviceOrderId.HasValue)
        {
            query = query.Where(p => p.ServiceOrderId == serviceOrderId.Value);
        }

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own payments only
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return Ok(new List<PaymentDto>());
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payments = await query.ToListAsync();
        return Ok(payments.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get a specific payment.
    /// Admin/Mechanic: any payment.
    /// Client: own payment only (IDOR).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid id)
    {
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Workshop)
            .Where(p => p.Id == id);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payment = await query.FirstOrDefaultAsync();
        if (payment == null) return NotFound();
        return Ok(MapToDto(payment));
    }

    /// <summary>
    /// Get payment for a specific service order.
    /// Admin/Mechanic: any order's payment.
    /// Client: own order's payment only (IDOR).
    /// </summary>
    [HttpGet("by-order/{serviceOrderId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetPaymentByOrder(Guid serviceOrderId)
    {
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Workshop)
            .Where(p => p.ServiceOrderId == serviceOrderId);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payment = await query.FirstOrDefaultAsync();
        if (payment == null) return NotFound();
        return Ok(MapToDto(payment));
    }

    /// <summary>
    /// Create a new payment invoice for a service order.
    /// Admin only. Server sets status=Pending, paymentDate=null, ownerId from ServiceOrder.Vehicle.OwnerId.
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentDto>> CreatePayment([FromBody] PaymentCreateDto dto)
    {
        var serviceOrder = await _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .FirstOrDefaultAsync(so => so.Id == dto.ServiceOrderId);

        if (serviceOrder == null)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Service order not found."
            });
        }

        // Check if payment already exists (one-to-one DB constraint)
        var alreadyExists = await _context.Payments
            .AnyAsync(p => p.ServiceOrderId == dto.ServiceOrderId);

        if (alreadyExists)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "A payment invoice already exists for this service order."
            });
        }

        var payment = new Payment
        {
            Amount = dto.Amount,
            Status = PaymentStatus.Pending,
            ServiceOrderId = dto.ServiceOrderId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var result = new PaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            Status = payment.Status,
            PaidAt = payment.PaidAt,
            ServiceOrderId = payment.ServiceOrderId,
            OwnerId = serviceOrder.Vehicle?.OwnerId,
            CreatedAt = payment.CreatedAt,
            VehicleInfo = serviceOrder.Vehicle != null
                ? $"{serviceOrder.Vehicle.Make} {serviceOrder.Vehicle.Model} ({serviceOrder.Vehicle.LicensePlate})"
                : null,
            WorkshopName = serviceOrder.Workshop?.Name.ToString()
        };

        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, result);
    }

    /// <summary>
    /// Mark a payment as paid (Status: Pending → Paid).
    /// Client (owner) only. IDOR: only own payments. Only if status=Pending.
    /// </summary>
    [HttpPatch("{id:guid}/pay")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "client")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayInvoice(Guid id)
    {
        var owner = await GetCurrentOwnerAsync();
        if (owner == null) return NotFound();

        var payment = await _context.Payments
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .FirstOrDefaultAsync(p => p.Id == id && p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);

        if (payment == null) return NotFound();

        if (payment.Status != PaymentStatus.Pending)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Payment is not in Pending status."
            });
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        _context.Entry(payment).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
