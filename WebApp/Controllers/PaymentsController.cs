using App.BLL;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Client;

namespace WebApp.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly IAppBll _bll;

    public PaymentsController(IAppBll bll)
    {
        _bll = bll;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /Payments — all roles
    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        IEnumerable<App.BLL.DTO.BllPayment> bllPayments;

        if (IsAdmin() || IsMechanic())
        {
            bllPayments = await _bll.Payments.AllWithDetailsAsync();
        }
        else
        {
            bllPayments = await _bll.Payments.AllByUserAsync(userId.Value);
        }

        var payments = bllPayments
            .OrderByDescending(p => p.CreatedAt)
            .Select(ToClientVm)
            .ToList();

        return View(new PaymentClientListViewModel { Payments = payments });
    }

    private static PaymentClientViewModel ToClientVm(App.BLL.DTO.BllPayment p) => new()
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
