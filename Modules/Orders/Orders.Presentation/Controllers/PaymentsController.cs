using Orders.Domain.Enums;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;
using Orders.Presentation.ViewModels.Client;

namespace Orders.Presentation.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /Payments — all roles
    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        IEnumerable<Orders.Application.DTO.BllPayment> bllPayments;

        if (IsAdmin() || IsMechanic())
        {
            bllPayments = await _paymentService.AllWithDetailsAsync();
        }
        else
        {
            bllPayments = await _paymentService.AllByUserAsync(userId.Value);
        }

        var payments = bllPayments
            .OrderByDescending(p => p.CreatedAt)
            .Select(ToClientVm)
            .ToList();

        return View(new PaymentClientListViewModel { Payments = payments });
    }

    private static PaymentClientViewModel ToClientVm(Orders.Application.DTO.BllPayment p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Status = p.Status,
        PaidAt = p.PaidAt,
        PaymentMethod = p.PaymentMethod,
        Notes = p.Notes,
        ServiceOrderId = p.ServiceOrderId,
        CreatedAt = p.CreatedAt
    };
}
