using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class MechanicsController : Controller
{
    private readonly IMediator _mediator;

    public MechanicsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var mechanics = await _mediator.Send(new GetAllMechanicsQuery());
        var vm = new MechanicListViewModel
        {
            Mechanics = mechanics.Select(m => new MechanicViewModel
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Phone = m.Phone,
                Email = m.Email,
                Specialization = m.Specialization
            }).ToList()
        };
        return View(vm);
    }

    public IActionResult Create()
    {
        return View(new MechanicViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MechanicViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        await _mediator.Send(new CreateMechanicCommand(
            vm.FirstName, vm.LastName, vm.Phone, vm.Email, vm.Specialization));

        TempData["Success"] = $"Mechanic {vm.FirstName} {vm.LastName} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var mechanic = await _mediator.Send(new GetMechanicByIdQuery(id));
        if (mechanic == null) return NotFound();

        return View(new MechanicViewModel
        {
            Id = mechanic.Id,
            FirstName = mechanic.FirstName,
            LastName = mechanic.LastName,
            Phone = mechanic.Phone,
            Email = mechanic.Email,
            Specialization = mechanic.Specialization
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MechanicViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var result = await _mediator.Send(new UpdateMechanicCommand(
            vm.Id, vm.FirstName, vm.LastName, vm.Phone, vm.Email, vm.Specialization));
        if (result == null) return NotFound();

        TempData["Success"] = $"Mechanic {vm.FirstName} {vm.LastName} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var mechanic = await _mediator.Send(new GetMechanicByIdQuery(id));
        if (mechanic == null) return NotFound();

        return View(new MechanicViewModel
        {
            Id = mechanic.Id,
            FirstName = mechanic.FirstName,
            LastName = mechanic.LastName,
            Phone = mechanic.Phone,
            Email = mechanic.Email,
            Specialization = mechanic.Specialization
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteMechanicCommand(id));
        if (!deleted) return NotFound();

        TempData["Success"] = "Mechanic deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
