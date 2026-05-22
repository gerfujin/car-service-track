using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class WorkshopsController : Controller
{
    private readonly IMediator _mediator;

    public WorkshopsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var workshops = await _mediator.Send(new GetAllWorkshopsQuery());
        var vm = new WorkshopListViewModel
        {
            Workshops = workshops.Select(w => new WorkshopViewModel
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                Phone = w.Phone,
                Email = w.Email
            }).ToList()
        };
        return View(vm);
    }

    public IActionResult Create()
    {
        return View(new WorkshopViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkshopViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        await _mediator.Send(new CreateWorkshopCommand(vm.Name, vm.Address, vm.Phone, vm.Email));

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var workshop = await _mediator.Send(new GetWorkshopByIdQuery(id));
        if (workshop == null) return NotFound();

        return View(new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name,
            Address = workshop.Address,
            Phone = workshop.Phone,
            Email = workshop.Email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, WorkshopViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var result = await _mediator.Send(new UpdateWorkshopCommand(vm.Id, vm.Name, vm.Address, vm.Phone, vm.Email));
        if (result == null) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var workshop = await _mediator.Send(new GetWorkshopByIdQuery(id));
        if (workshop == null) return NotFound();

        return View(new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name,
            Address = workshop.Address,
            Phone = workshop.Phone,
            Email = workshop.Email
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteWorkshopCommand(id));
        if (!deleted) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var workshop = await _mediator.Send(new GetWorkshopByIdQuery(id));
        if (workshop == null) return NotFound();

        return View(new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name,
            Address = workshop.Address,
            Phone = workshop.Phone,
            Email = workshop.Email
        });
    }
}
