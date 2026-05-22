using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Contracts.Queries;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _mediator.Send(new GetAdminDashboardStatsQuery());

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
