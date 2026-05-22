using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class ServicesController : Controller
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _mediator.Send(new GetAllServicesQuery());
        var vm = new ServiceAdminListViewModel
        {
            Services = services.Select(s => new ServiceAdminViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description ?? string.Empty,
                BasePrice = s.BasePrice,
                EstimatedTimeMinutes = s.EstimatedTimeMinutes
            }).ToList()
        };
        return View(vm);
    }

    public IActionResult Create()
    {
        return View(new ServiceAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceAdminViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        await _mediator.Send(new CreateServiceCommand(
            vm.Name, vm.Description, vm.BasePrice, vm.EstimatedTimeMinutes));

        TempData["Success"] = $"Service '{vm.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _mediator.Send(new GetServiceByIdQuery(id));
        if (service == null) return NotFound();

        return View(new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description ?? string.Empty,
            BasePrice = service.BasePrice,
            EstimatedTimeMinutes = service.EstimatedTimeMinutes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var result = await _mediator.Send(new UpdateServiceCommand(
            vm.Id, vm.Name, vm.Description, vm.BasePrice, vm.EstimatedTimeMinutes));
        if (result == null) return NotFound();

        TempData["Success"] = $"Service '{vm.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _mediator.Send(new GetServiceByIdQuery(id));
        if (service == null) return NotFound();

        return View(new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description ?? string.Empty,
            BasePrice = service.BasePrice,
            EstimatedTimeMinutes = service.EstimatedTimeMinutes
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteServiceCommand(id));
        if (!deleted) return NotFound();

        TempData["Success"] = "Service deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var service = await _mediator.Send(new GetServiceByIdQuery(id));
        if (service == null) return NotFound();

        return View(new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description ?? string.Empty,
            BasePrice = service.BasePrice,
            EstimatedTimeMinutes = service.EstimatedTimeMinutes
        });
    }
}
