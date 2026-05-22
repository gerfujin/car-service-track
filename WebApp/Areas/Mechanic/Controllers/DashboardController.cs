using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;
using WebApp.Areas.Mechanic.ViewModels;

namespace WebApp.Areas.Mechanic.Controllers;

[Area("Mechanic")]
[Authorize(Roles = "mechanic")]
public class DashboardController : Controller
{
    private readonly IServiceOrderService _serviceOrders;

    public DashboardController(IServiceOrderService serviceOrders)
    {
        _serviceOrders = serviceOrders;
    }

    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return Challenge();

        var stats = await _serviceOrders.GetMechanicDashboardStatsAsync(userId.Value);

        var vm = new MechanicDashboardViewModel
        {
            TotalOrders = stats.TotalServiceOrders,
            PendingOrders = stats.PendingOrders,
            InProgressOrders = stats.InProgressOrders,
            CompletedOrders = stats.CompletedOrders,
            TotalVehicles = stats.TotalVehicles,
            TotalPayments = stats.TotalPayments
        };

        return View(vm);
    }
}
