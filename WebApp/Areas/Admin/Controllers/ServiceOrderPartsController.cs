using App.BLL;
using App.BLL.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;
using WebApp.Helpers;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class ServiceOrderPartsController : Controller
{
    private readonly IAppBll _appBll;

    public ServiceOrderPartsController(IAppBll appBll)
    {
        _appBll = appBll;
    }

    public async Task<IActionResult> Index(Guid? serviceOrderId)
    {
        var allParts = await _appBll.ServiceOrderParts.AllAsync();
        var parts = allParts
            .Where(x => !serviceOrderId.HasValue || x.ServiceOrderId == serviceOrderId.Value)
            .Select(x => new ServiceOrderPartAdminViewModel
            {
                Id = x.Id,
                ServiceOrderId = x.ServiceOrderId,
                SparePartId = x.SparePartId,
                Quantity = x.Quantity,
                Price = x.UnitPrice,
                SparePartName = x.SparePartName
            })
            .ToList();

        return View(new ServiceOrderPartAdminListViewModel
        {
            ServiceOrderId = serviceOrderId,
            Parts = parts
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _appBll.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        return View(new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice,
            SparePartName = entity.SparePartName
        });
    }

    public async Task<IActionResult> Create(Guid? serviceOrderId)
    {
        var vm = new ServiceOrderPartAdminViewModel
        {
            ServiceOrderId = serviceOrderId ?? Guid.Empty,
            Quantity = 1
        };
        await PopulateOptions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceOrderPartAdminViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptions(vm);
            return View(vm);
        }

        var order = await _appBll.ServiceOrders.FindAsync(vm.ServiceOrderId);
        var sparePart = await _appBll.SpareParts.FindAsync(vm.SparePartId);
        if (order == null || sparePart == null)
        {
            TempData["Error"] = "Service order or spare part not found.";
            await PopulateOptions(vm);
            return View(vm);
        }

        var entity = new BllServiceOrderPart
        {
            ServiceOrderId = vm.ServiceOrderId,
            SparePartId = vm.SparePartId,
            Quantity = vm.Quantity,
            UnitPrice = vm.Price <= 0 ? sparePart.UnitPrice : vm.Price
        };

        _appBll.ServiceOrderParts.Add(entity);
        await _appBll.ServiceOrderParts.RecalculateOrderTotalAsync(entity.ServiceOrderId);
        await _appBll.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { serviceOrderId = vm.ServiceOrderId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _appBll.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        var vm = new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice
        };

        await PopulateOptions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceOrderPartAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateOptions(vm);
            return View(vm);
        }

        var existing = await _appBll.ServiceOrderParts.FindAsync(id);
        var sparePart = await _appBll.SpareParts.FindAsync(vm.SparePartId);
        if (existing == null || sparePart == null) return NotFound();

        var entity = new BllServiceOrderPart
        {
            Id = vm.Id,
            ServiceOrderId = vm.ServiceOrderId,
            SparePartId = vm.SparePartId,
            Quantity = vm.Quantity,
            UnitPrice = vm.Price <= 0 ? sparePart.UnitPrice : vm.Price
        };

        var result = await _appBll.ServiceOrderParts.UpdateAsync(entity);
        if (result == null) return NotFound();

        await _appBll.ServiceOrderParts.RecalculateOrderTotalAsync(vm.ServiceOrderId);
        await _appBll.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { serviceOrderId = vm.ServiceOrderId });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _appBll.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        return View(new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice,
            SparePartName = entity.SparePartName
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var entity = await _appBll.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        var orderId = entity.ServiceOrderId;
        _appBll.ServiceOrderParts.Remove(entity);
        await _appBll.ServiceOrderParts.RecalculateOrderTotalAsync(orderId);
        await _appBll.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { serviceOrderId = orderId });
    }

    private async Task PopulateOptions(ServiceOrderPartAdminViewModel vm)
    {
        vm.ServiceOrderOptions = (await _appBll.ServiceOrders.GetSelectListForAdminAsync()).ToSelectListItems();
        vm.SparePartOptions = (await _appBll.SpareParts.GetSelectListAsync()).ToSelectListItems();
    }
}
