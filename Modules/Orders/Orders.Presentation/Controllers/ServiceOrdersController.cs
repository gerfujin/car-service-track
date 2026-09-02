using Orders.Domain.Enums;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Services;
using WebApp.ViewModels.Client;

namespace WebApp.Controllers;

[Authorize]
public class ServiceOrdersController : Controller
{
    private readonly IServiceOrderService _serviceOrders;

    public ServiceOrdersController(IServiceOrderService serviceOrders)
    {
        _serviceOrders = serviceOrders;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /ServiceOrders — all roles
    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        IEnumerable<Orders.Application.DTO.BllServiceOrder> bllOrders;

        if (IsAdmin() || IsMechanic())
        {
            bllOrders = await _serviceOrders.AllWithDetailsAsync();
        }
        else
        {
            bllOrders = await _serviceOrders.AllByUserAsync(userId.Value);
        }

        var orders = bllOrders
            .OrderByDescending(o => o.OrderDate)
            .Select(ToClientVm)
            .ToList();

        var canUpdateStatus = IsAdmin() || IsMechanic();
        var vm = new ServiceOrderClientListViewModel { Orders = orders, CanUpdateStatus = canUpdateStatus };

        return View(vm);
    }

    private static ServiceOrderClientViewModel ToClientVm(Orders.Application.DTO.BllServiceOrder so) => new()
    {
        Id = so.Id,
        Description = so.Description,
        Status = so.Status,
        OrderDate = so.OrderDate,
        CompletedDate = so.CompletedDate,
        VehicleDisplay = so.VehicleDisplay ?? "N/A",
        WorkshopName = so.WorkshopName ?? "N/A",
        MechanicName = so.MechanicName,
        TotalAmount = so.TotalAmount,
        HasPayment = so.HasPayment
    };
}
