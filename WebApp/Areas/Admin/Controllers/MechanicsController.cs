using App.BLL;
using App.BLL.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class MechanicsController : Controller
{
    private readonly IAppBll _appBll;

    public MechanicsController(IAppBll appBll)
    {
        _appBll = appBll;
    }

    public async Task<IActionResult> Index()
    {
        var mechanics = await _appBll.Mechanics.AllAsync();
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

        var mechanic = new BllMechanic
        {
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            Phone = vm.Phone,
            Email = vm.Email,
            Specialization = vm.Specialization
        };

        _appBll.Mechanics.Add(mechanic);
        await _appBll.SaveChangesAsync();

        TempData["Success"] = $"Mechanic {vm.FirstName} {vm.LastName} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var mechanic = await _appBll.Mechanics.FindAsync(id);
        if (mechanic == null) return NotFound();

        var vm = new MechanicViewModel
        {
            Id = mechanic.Id,
            FirstName = mechanic.FirstName,
            LastName = mechanic.LastName,
            Phone = mechanic.Phone,
            Email = mechanic.Email,
            Specialization = mechanic.Specialization
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MechanicViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var existing = await _appBll.Mechanics.FindAsync(id);
        if (existing == null) return NotFound();

        var mechanic = new BllMechanic
        {
            Id = vm.Id,
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            Phone = vm.Phone,
            Email = vm.Email,
            Specialization = vm.Specialization
        };

        var result = await _appBll.Mechanics.UpdateAsync(mechanic);
        if (result == null) return NotFound();

        TempData["Success"] = $"Mechanic {vm.FirstName} {vm.LastName} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var mechanic = await _appBll.Mechanics.FindAsync(id);
        if (mechanic == null) return NotFound();

        var vm = new MechanicViewModel
        {
            Id = mechanic.Id,
            FirstName = mechanic.FirstName,
            LastName = mechanic.LastName,
            Phone = mechanic.Phone,
            Email = mechanic.Email,
            Specialization = mechanic.Specialization
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var mechanic = await _appBll.Mechanics.FindAsync(id);
        if (mechanic == null) return NotFound();

        _appBll.Mechanics.Remove(mechanic);
        await _appBll.SaveChangesAsync();

        TempData["Success"] = "Mechanic deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
