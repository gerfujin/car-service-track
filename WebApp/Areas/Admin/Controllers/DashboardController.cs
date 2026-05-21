using App.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class DashboardController : Controller
{
    private readonly IAppBll _bll;

    public DashboardController(IAppBll bll)
    {
        _bll = bll;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _bll.ServiceOrders.GetAdminDashboardStatsAsync();

        var vm = new DashboardViewModel
        {
            TotalVehicles = stats.TotalVehicles,
            TotalServiceOrders = stats.TotalServiceOrders,
            TotalWorkshops = stats.TotalWorkshops,
            TotalMechanics = stats.TotalMechanics,
            TotalClients = stats.TotalClients,
            PendingOrders = stats.PendingOrders,
            InProgressOrders = stats.InProgressOrders,
            CompletedOrders = stats.CompletedOrders,
            TotalRevenue = stats.TotalRevenue
        };

        return View(vm);
    }
}
