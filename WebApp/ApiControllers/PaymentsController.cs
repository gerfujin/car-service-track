using App.BLL;
using App.BLL.DTO;
using App.Domain.Enums;
using App.DTO.v1;
using App.DTO.v1.Payment;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsController : ControllerBase
{
    private readonly IAppBll _bll;

    public PaymentsController(IAppBll bll)
    {
        _bll = bll;
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
        IEnumerable<BllPayment> payments;
        if (!IsAdmin() && !IsMechanic())
        {
            // Client: IDOR — own payments only. Empty list returned when no owner profile exists.
            payments = await _bll.Payments.AllByUserAsync(GetCurrentUserId(), serviceOrderId);
        }
        else
        {
            payments = await _bll.Payments.AllWithDetailsAsync(serviceOrderId);
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
        BllPayment? payment;
        if (!IsAdmin() && !IsMechanic())
        {
            payment = await _bll.Payments.FindWithDetailsForUserAsync(id, GetCurrentUserId());
        }
        else
        {
            payment = await _bll.Payments.FindWithDetailsAsync(id);
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
        BllPayment? payment;
        if (!IsAdmin() && !IsMechanic())
        {
            payment = await _bll.Payments.FindByServiceOrderForUserAsync(serviceOrderId, GetCurrentUserId());
        }
        else
        {
            payment = await _bll.Payments.FindByServiceOrderAsync(serviceOrderId);
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
        var (bllPayment, error) = await _bll.Payments.CreateForServiceOrderAsync(dto.ServiceOrderId, dto.Amount);
        if (error != null)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = error
            });
        }
        await _bll.SaveChangesAsync();
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
        var payment = await _bll.Payments.FindWithDetailsForUserAsync(id, GetCurrentUserId());
        if (payment == null) return NotFound();

        if (payment.Status != PaymentStatus.Pending)
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Payment is not in Pending status."
            });
        }

        await _bll.Payments.MarkAsPaidAsync(payment.Id);
        await _bll.SaveChangesAsync();
        return NoContent();
    }
}
