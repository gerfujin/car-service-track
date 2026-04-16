using App.DAL.EF;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            TotalVehicles = await _context.Vehicles.CountAsync(),
            TotalServiceOrders = await _context.ServiceOrders.CountAsync(),
            TotalWorkshops = await _context.Workshops.CountAsync(),
            TotalMechanics = await _context.Mechanics.CountAsync(),
            TotalClients = await _context.Owners.CountAsync(),
            PendingOrders = await _context.ServiceOrders
                .CountAsync(so => so.Status == ServiceOrderStatus.Pending),
            InProgressOrders = await _context.ServiceOrders
                .CountAsync(so => so.Status == ServiceOrderStatus.InProgress),
            CompletedOrders = await _context.ServiceOrders
                .CountAsync(so => so.Status == ServiceOrderStatus.Completed),
            TotalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => p.Amount)
        };

        return View(vm);
    }
}
