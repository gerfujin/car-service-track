using App.DAL.EF;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Mechanic.ViewModels;

namespace WebApp.Areas.Mechanic.Controllers;

[Area("Mechanic")]
[Authorize(Roles = "mechanic")]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new MechanicDashboardViewModel
        {
            TotalOrders = await _context.ServiceOrders.CountAsync(),
            PendingOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.Pending),
            InProgressOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.InProgress),
            CompletedOrders = await _context.ServiceOrders.CountAsync(so => so.Status == ServiceOrderStatus.Completed),
            TotalVehicles = await _context.Vehicles.CountAsync(),
            TotalPayments = await _context.Payments.CountAsync()
        };

        return View(vm);
    }
}
