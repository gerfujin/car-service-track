using App.BLL;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels;
using WebApp.Helpers;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin,mechanic")]
public class ServiceOrdersController : Controller
{
    private readonly IAppBll _appBll;

    public ServiceOrdersController(IAppBll appBll)
    {
        _appBll = appBll;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _appBll.ServiceOrders.AllWithDetailsAsync();
        var vm = new ServiceOrderAdminListViewModel
        {
            Orders = orders.Select(so => new ServiceOrderAdminViewModel
            {
                Id = so.Id,
                Description = so.Description,
                Status = so.Status,
                OrderDate = so.OrderDate,
                CompletedDate = so.CompletedDate,
                VehicleDisplay = so.VehicleDisplay ?? "N/A",
                OwnerName = so.OwnerName ?? "N/A",
                WorkshopName = so.WorkshopName ?? "N/A",
                MechanicName = so.MechanicName,
                TotalAmount = so.TotalAmount,
                FinalPrice = so.FinalPrice,
                HasPayment = so.HasPayment
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> UpdateStatus(Guid id)
    {
        var order = await _appBll.ServiceOrders.FindWithDetailsAsync(id);

        if (order == null) return NotFound();

        var mechanics = (await _appBll.Mechanics.GetSelectListAsync()).ToSelectListItems();

        var vm = new ServiceOrderStatusUpdateViewModel
        {
            Id = order.Id,
            CurrentStatus = order.Status,
            NewStatus = order.Status,
            MechanicId = order.MechanicId,
            FinalPrice = order.FinalPrice,
            StatusOptions = Enum.GetValues<ServiceOrderStatus>()
                .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList(),
            MechanicOptions = mechanics
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, ServiceOrderStatusUpdateViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            vm.StatusOptions = Enum.GetValues<ServiceOrderStatus>()
                .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList();
            vm.MechanicOptions = (await _appBll.Mechanics.GetSelectListAsync()).ToSelectListItems();
            return View(vm);
        }

        var result = await _appBll.ServiceOrders.UpdateStatusForAdminAsync(id, vm.NewStatus, vm.MechanicId, vm.FinalPrice, vm.Notes);
        if (!result) return NotFound();

        await _appBll.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
