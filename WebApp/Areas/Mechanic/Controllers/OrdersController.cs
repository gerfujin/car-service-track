using App.BLL;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Mechanic.ViewModels;

namespace WebApp.Areas.Mechanic.Controllers;

[Area("Mechanic")]
[Authorize(Roles = "mechanic")]
public class OrdersController : Controller
{
    private readonly IAppBll _bll;

    public OrdersController(IAppBll bll)
    {
        _bll = bll;
    }

    // GET: /Mechanic/Orders
    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return Challenge();

        var bllOrders = await _bll.ServiceOrders.AllByMechanicAsync(userId.Value);

        var orders = bllOrders
            .OrderByDescending(o => o.OrderDate)
            .Select(ToMechanicVm)
            .ToList();

        return View(new MechanicOrderListViewModel { Orders = orders });
    }

    // GET: /Mechanic/Orders/UpdateStatus/{id}
    public async Task<IActionResult> UpdateStatus(Guid id)
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return Challenge();

        var order = await _bll.ServiceOrders.FindByMechanicAsync(id, userId.Value);
        if (order == null) return NotFound();

        var vm = new MechanicUpdateStatusViewModel
        {
            Id = order.Id,
            CurrentStatus = order.Status,
            VehicleDisplay = order.VehicleDisplay ?? "N/A",
            WorkshopName = order.WorkshopName ?? "N/A",
            Description = order.Description
        };

        return View(vm);
    }

    // POST: /Mechanic/Orders/UpdateStatus/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, MechanicUpdateStatusViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return Challenge();

        var result = await _bll.ServiceOrders.UpdateStatusByMechanicAsync(id, userId.Value, vm.NewStatus, vm.Notes);
        if (!result) return NotFound();

        await _bll.SaveChangesAsync();

        TempData["Success"] = $"Order status updated to {vm.NewStatus}.";
        return RedirectToAction(nameof(Index));
    }

    private static MechanicOrderViewModel ToMechanicVm(App.BLL.DTO.BllServiceOrder so) => new()
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
