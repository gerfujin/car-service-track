using App.BLL;
using App.BLL.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class WorkshopsController : Controller
{
    private readonly IAppBll _appBll;

    public WorkshopsController(IAppBll appBll)
    {
        _appBll = appBll;
    }

    public async Task<IActionResult> Index()
    {
        var workshops = await _appBll.Workshops.AllAsync();
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

        var workshop = new BllWorkshop
        {
            Name = vm.Name,
            Address = vm.Address,
            Phone = vm.Phone,
            Email = vm.Email
        };

        _appBll.Workshops.Add(workshop);
        await _appBll.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var workshop = await _appBll.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        var vm = new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name,
            Address = workshop.Address,
            Phone = workshop.Phone,
            Email = workshop.Email
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, WorkshopViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var existing = await _appBll.Workshops.FindAsync(id);
        if (existing == null) return NotFound();

        var workshop = new BllWorkshop
        {
            Id = vm.Id,
            Name = vm.Name,
            Address = vm.Address,
            Phone = vm.Phone,
            Email = vm.Email
        };

        var result = await _appBll.Workshops.UpdateAsync(workshop);
        if (result == null) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var workshop = await _appBll.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        var vm = new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name,
            Address = workshop.Address,
            Phone = workshop.Phone,
            Email = workshop.Email
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var workshop = await _appBll.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        _appBll.Workshops.Remove(workshop);
        await _appBll.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var workshop = await _appBll.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        var vm = new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name,
            Address = workshop.Address,
            Phone = workshop.Phone,
            Email = workshop.Email
        };

        return View(vm);
    }
}
