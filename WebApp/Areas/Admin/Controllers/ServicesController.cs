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
public class ServicesController : Controller
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _context.Services
            .Select(s => new ServiceAdminViewModel
            {
                Id = s.Id,
                Name = s.Name.ToString(),
                Description = s.Description.ToString(),
                BasePrice = s.BasePrice
            })
            .ToListAsync();

        return View(new ServiceAdminListViewModel { Services = services });
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

        var service = new Service
        {
            Name = new LangStr(vm.Name),
            Description = new LangStr(vm.Description),
            BasePrice = vm.BasePrice
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Service '{vm.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        var vm = new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name.ToString(),
            Description = service.Description.ToString(),
            BasePrice = service.BasePrice
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        service.Name = new LangStr(vm.Name);
        service.Description = new LangStr(vm.Description);
        service.BasePrice = vm.BasePrice;
        service.UpdatedAt = DateTime.UtcNow;

        _context.Entry(service).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Service '{vm.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        var vm = new ServiceAdminViewModel
        {
            Id = service.Id,
            Name = service.Name.ToString(),
            Description = service.Description.ToString(),
            BasePrice = service.BasePrice
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Service deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
