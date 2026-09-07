using App.DTO.v1;
using App.DTO.v1.Payment;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Domain.Enums;
using Orders.Presentation.Mappers;

namespace Orders.Presentation.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IOrdersUnitOfWork _ordersUow;

    public PaymentsController(IPaymentService payments, IOrdersUnitOfWork ordersUow)
    {
        _payments = payments;
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
        IEnumerable<Orders.Application.DTO.BllPayment> payments;
        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own payments only. Empty list returned when no owner profile exists.
            payments = await _payments.AllByUserAsync(GetCurrentUserId(), serviceOrderId);
        }
        else
        {
            payments = await _payments.AllWithDetailsAsync(serviceOrderId);
        }
        return Ok(payments.Select(PaymentApiMapper.ToApiDto).ToList());
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
        Orders.Application.DTO.BllPayment? payment;
        if (!IsAdmin() && !IsMechanic())
        {
            payment = await _payments.FindWithDetailsForUserAsync(id, GetCurrentUserId());
        }
        else
        {
            payment = await _payments.FindWithDetailsAsync(id);
        }
        if (payment == null) return NotFound();
        return Ok(PaymentApiMapper.ToApiDto(payment));
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
        Orders.Application.DTO.BllPayment? payment;
        if (!IsAdmin() && !IsMechanic())
        {
            payment = await _payments.FindByServiceOrderForUserAsync(serviceOrderId, GetCurrentUserId());
        }
        else
        {
            payment = await _payments.FindByServiceOrderAsync(serviceOrderId);
        }
        if (payment == null) return NotFound();
        return Ok(PaymentApiMapper.ToApiDto(payment));
    }

    /// <summary>
    /// Create a new payment invoice for a service order.
    /// Admin only. Server sets Status=Pending; one payment per ServiceOrder enforced.
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentDto>> CreatePayment([FromBody] PaymentCreateDto dto)
    {
        var (bllPayment, error) = await _payments.CreateForServiceOrderAsync(dto.ServiceOrderId, dto.Amount);
        if (error != null)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = error
            });
        }
        await _ordersUow.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPayment), new { id = bllPayment!.Id }, PaymentApiMapper.ToApiDto(bllPayment));
    }

    /// <summary>
    /// Mark a payment as paid (Status: Pending → Paid).
    /// Client (owner) only. IDOR: only own payments. Only if Status=Pending.
    /// </summary>
    [HttpPatch("{id:guid}/pay")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "client")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayInvoice(Guid id)
    {
        var payment = await _payments.FindWithDetailsForUserAsync(id, GetCurrentUserId());
        if (payment == null) return NotFound();

        if (payment.Status != PaymentStatus.Pending)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Payment is not in Pending status."
            });
        }

        await _payments.MarkAsPaidAsync(payment.Id);
        await _ordersUow.SaveChangesAsync();
        return NoContent();
    }
}
