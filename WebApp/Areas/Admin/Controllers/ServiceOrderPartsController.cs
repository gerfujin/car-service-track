using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orders.Contracts.Commands;
using Orders.Contracts.Queries;
using WebApp.Areas.Admin.ViewModels;
using Workshops.Contracts.Queries;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class ServiceOrderPartsController : Controller
{
    private readonly IMediator _mediator;

    public ServiceOrderPartsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(Guid? serviceOrderId)
    {
        var parts = await _mediator.Send(new GetAllServiceOrderPartsQuery(serviceOrderId));
        return View(new ServiceOrderPartAdminListViewModel
        {
            ServiceOrderId = serviceOrderId,
            Parts = parts.Select(x => new ServiceOrderPartAdminViewModel
            {
                Id = x.Id,
                ServiceOrderId = x.ServiceOrderId,
                SparePartId = x.SparePartId,
                Quantity = x.Quantity,
                Price = x.UnitPrice,
                SparePartName = x.SparePartName
            }).ToList()
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _mediator.Send(new GetServiceOrderPartByIdQuery(id));
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
        await PopulateOptionsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceOrderPartAdminViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(vm);
            return View(vm);
        }

        var result = await _mediator.Send(new CreateServiceOrderPartCommand(
            vm.ServiceOrderId, vm.SparePartId, vm.Quantity, vm.Price));

        if (result == null)
        {
            TempData["Error"] = "Service order or spare part not found.";
            await PopulateOptionsAsync(vm);
            return View(vm);
        }

        return RedirectToAction(nameof(Index), new { serviceOrderId = vm.ServiceOrderId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _mediator.Send(new GetServiceOrderPartByIdQuery(id));
        if (entity == null) return NotFound();

        var vm = new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice
        };
        await PopulateOptionsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceOrderPartAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(vm);
            return View(vm);
        }

        var result = await _mediator.Send(new UpdateServiceOrderPartCommand(
            vm.Id, vm.ServiceOrderId, vm.SparePartId, vm.Quantity, vm.Price));

        if (result == null) return NotFound();

        return RedirectToAction(nameof(Index), new { serviceOrderId = vm.ServiceOrderId });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _mediator.Send(new GetServiceOrderPartByIdQuery(id));
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
        // Capture the service order ID before deletion for the redirect.
        var entity = await _mediator.Send(new GetServiceOrderPartByIdQuery(id));
        var serviceOrderId = entity?.ServiceOrderId;

        var deleted = await _mediator.Send(new DeleteServiceOrderPartCommand(id));
        if (!deleted) return NotFound();

        return RedirectToAction(nameof(Index), new { serviceOrderId });
    }

    private async Task PopulateOptionsAsync(ServiceOrderPartAdminViewModel vm)
    {
        var orderItems = await _mediator.Send(new GetServiceOrderSelectListQuery());
        vm.ServiceOrderOptions = orderItems
            .Select(i => new SelectListItem { Value = i.Value, Text = i.Text })
            .ToList();

        var spareParts = await _mediator.Send(new GetAllSparePartsQuery());
        vm.SparePartOptions = spareParts
            .Select(sp => new SelectListItem
            {
                Value = sp.Id.ToString(),
                Text = string.IsNullOrEmpty(sp.PartNumber) ? sp.Name : $"{sp.Name} ({sp.PartNumber})"
            })
            .ToList();
    }
}
