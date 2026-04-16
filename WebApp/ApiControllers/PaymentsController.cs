using App.DAL.EF;
using App.Domain;
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

    /// <summary>
    /// Get payments.
    /// Admin/Mechanic: all payments.
    /// Client: own payments only (IDOR via vehicle ownership).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<PaymentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments()
    {
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle);

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own payments only
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return Ok(new List<PaymentDto>());
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payments = await query
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Status = p.Status,
                PaidAt = p.PaidAt,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                ServiceOrderId = p.ServiceOrderId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(payments);
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
            .Where(p => p.Id == id);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payment = await query
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Status = p.Status,
                PaidAt = p.PaidAt,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                ServiceOrderId = p.ServiceOrderId,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (payment == null) return NotFound();
        return Ok(payment);
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
            .Where(p => p.ServiceOrderId == serviceOrderId);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payment = await query
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Status = p.Status,
                PaidAt = p.PaidAt,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                ServiceOrderId = p.ServiceOrderId,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (payment == null) return NotFound();
        return Ok(payment);
    }
}
