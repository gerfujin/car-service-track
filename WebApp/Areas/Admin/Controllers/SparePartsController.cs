using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class SparePartsController : Controller
{
    private readonly IMediator _mediator;

    public SparePartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var parts = await _mediator.Send(new GetAllSparePartsQuery());
        var vm = new SparePartAdminListViewModel
        {
            PageTitle = "Spare Parts",
            SpareParts = parts.Select(sp => new SparePartAdminViewModel
            {
                Id = sp.Id,
                Name = sp.Name,
                PartNumber = sp.PartNumber,
                UnitPrice = sp.UnitPrice,
                StockQuantity = sp.StockQuantity
            }).ToList()
        };
        return View(vm);
    }

    public IActionResult Create()
    {
        return View(new SparePartAdminViewModel { PageTitle = "Add Spare Part" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SparePartAdminViewModel vm)
    {
        if (!ModelState.IsValid) { vm.PageTitle = "Add Spare Part"; return View(vm); }

        await _mediator.Send(new CreateSparePartCommand(
            vm.Name ?? "", vm.PartNumber, vm.UnitPrice, vm.StockQuantity));

        TempData["Success"] = $"Spare part '{vm.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var part = await _mediator.Send(new GetSparePartByIdQuery(id));
        if (part == null) return NotFound();

        return View(new SparePartAdminViewModel
        {
            PageTitle = "Edit Spare Part",
            Id = part.Id,
            Name = part.Name,
            PartNumber = part.PartNumber,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SparePartAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) { vm.PageTitle = "Edit Spare Part"; return View(vm); }

        var result = await _mediator.Send(new UpdateSparePartCommand(
            vm.Id, vm.Name ?? "", vm.PartNumber, vm.UnitPrice, vm.StockQuantity));
        if (result == null) return NotFound();

        TempData["Success"] = $"Spare part '{vm.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var part = await _mediator.Send(new GetSparePartByIdQuery(id));
        if (part == null) return NotFound();

        return View(new SparePartAdminViewModel
        {
            PageTitle = "Delete Spare Part",
            Id = part.Id,
            Name = part.Name,
            PartNumber = part.PartNumber,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteSparePartCommand(id));
        if (!deleted) return NotFound();

        TempData["Success"] = "Spare part deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
