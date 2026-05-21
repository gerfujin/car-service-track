using App.BLL;
using App.BLL.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class ServicesController : Controller
{
    private readonly IAppBll _appBll;

    public ServicesController(IAppBll appBll)
    {
        _appBll = appBll;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _appBll.Services.AllAsync();
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

        var service = new BllService
        {
            Name = vm.Name,
            Description = vm.Description,
            BasePrice = vm.BasePrice,
            EstimatedTimeMinutes = vm.EstimatedTimeMinutes
        };

        _appBll.Services.Add(service);
        await _appBll.SaveChangesAsync();

        TempData["Success"] = $"Service '{vm.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _appBll.Services.FindAsync(id);
        if (service == null) return NotFound();

        var vm = new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description ?? string.Empty,
            BasePrice = service.BasePrice,
            EstimatedTimeMinutes = service.EstimatedTimeMinutes
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var existing = await _appBll.Services.FindAsync(id);
        if (existing == null) return NotFound();

        var service = new BllService
        {
            Id = vm.Id,
            Name = vm.Name,
            Description = vm.Description,
            BasePrice = vm.BasePrice,
            EstimatedTimeMinutes = vm.EstimatedTimeMinutes
        };

        var result = await _appBll.Services.UpdateAsync(service);
        if (result == null) return NotFound();

        TempData["Success"] = $"Service '{vm.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _appBll.Services.FindAsync(id);
        if (service == null) return NotFound();

        var vm = new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description ?? string.Empty,
            BasePrice = service.BasePrice,
            EstimatedTimeMinutes = service.EstimatedTimeMinutes
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var service = await _appBll.Services.FindAsync(id);
        if (service == null) return NotFound();

        _appBll.Services.Remove(service);
        await _appBll.SaveChangesAsync();

        TempData["Success"] = "Service deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var service = await _appBll.Services.FindAsync(id);
        if (service == null) return NotFound();

        var vm = new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description ?? string.Empty,
            BasePrice = service.BasePrice,
            EstimatedTimeMinutes = service.EstimatedTimeMinutes
        };

        return View(vm);
    }
}
