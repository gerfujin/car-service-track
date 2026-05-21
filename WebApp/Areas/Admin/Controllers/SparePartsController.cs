using App.BLL;
using App.BLL.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class SparePartsController : Controller
{
    private readonly IAppBll _appBll;

    public SparePartsController(IAppBll appBll)
    {
        _appBll = appBll;
    }

    public async Task<IActionResult> Index()
    {
        var entities = await _appBll.SpareParts.AllAsync();
        var parts = entities.Select(sp => new SparePartAdminViewModel
        {
            Id = sp.Id,
            Name = sp.Name,
            PartNumber = sp.PartNumber,
            UnitPrice = sp.UnitPrice,
            StockQuantity = sp.StockQuantity
        }).ToList();

        return View(new SparePartAdminListViewModel { SpareParts = parts });
    }

    public IActionResult Create()
    {
        return View(new SparePartAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SparePartAdminViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var part = new BllSparePart
        {
            Name = vm.Name ?? "",
            PartNumber = vm.PartNumber,
            UnitPrice = vm.UnitPrice,
            StockQuantity = vm.StockQuantity
        };

        _appBll.SpareParts.Add(part);
        await _appBll.SaveChangesAsync();

        TempData["Success"] = $"Spare part '{vm.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var part = await _appBll.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        var vm = new SparePartAdminViewModel
        {
            Id = part.Id,
            Name = part.Name,
            PartNumber = part.PartNumber,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SparePartAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var existing = await _appBll.SpareParts.FindAsync(id);
        if (existing == null) return NotFound();

        var part = new BllSparePart
        {
            Id = vm.Id,
            Name = vm.Name ?? "",
            PartNumber = vm.PartNumber,
            UnitPrice = vm.UnitPrice,
            StockQuantity = vm.StockQuantity
        };

        var result = await _appBll.SpareParts.UpdateAsync(part);
        if (result == null) return NotFound();

        TempData["Success"] = $"Spare part '{vm.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var part = await _appBll.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        var vm = new SparePartAdminViewModel
        {
            Id = part.Id,
            Name = part.Name,
            PartNumber = part.PartNumber,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var part = await _appBll.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        _appBll.SpareParts.Remove(part);
        await _appBll.SaveChangesAsync();

        TempData["Success"] = "Spare part deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
