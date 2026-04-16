using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class SparePartsController : Controller
{
    private readonly AppDbContext _context;

    public SparePartsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var entities = await _context.SpareParts.ToListAsync();
        var parts = entities.Select(sp => new SparePartAdminViewModel
        {
            Id = sp.Id,
            Name = sp.Name.Translate() ?? sp.Name.ToString() ?? "",
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

        var part = new SparePart
        {
            Name = new LangStr(vm.Name ?? ""),
            PartNumber = vm.PartNumber,
            UnitPrice = vm.UnitPrice,
            StockQuantity = vm.StockQuantity
        };

        _context.SpareParts.Add(part);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Spare part '{vm.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var part = await _context.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        var vm = new SparePartAdminViewModel
        {
            Id = part.Id,
            Name = part.Name.Translate() ?? part.Name.ToString() ?? "",
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

        var part = await _context.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        part.Name = new LangStr(vm.Name ?? "");
        part.PartNumber = vm.PartNumber;
        part.UnitPrice = vm.UnitPrice;
        part.StockQuantity = vm.StockQuantity;
        part.UpdatedAt = DateTime.UtcNow;

        _context.Entry(part).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Spare part '{vm.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var part = await _context.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        var vm = new SparePartAdminViewModel
        {
            Id = part.Id,
            Name = part.Name.Translate() ?? part.Name.ToString() ?? "",
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
        var part = await _context.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        _context.SpareParts.Remove(part);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Spare part deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
