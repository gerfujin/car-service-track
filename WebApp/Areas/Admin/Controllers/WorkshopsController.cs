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
public class WorkshopsController : Controller
{
    private readonly AppDbContext _context;

    public WorkshopsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var workshops = await _context.Workshops
            .Select(w => new WorkshopViewModel
            {
                Id = w.Id,
                Name = w.Name.ToString(),
                Address = w.Address.ToString(),
                Phone = w.Phone,
                Email = w.Email
            })
            .ToListAsync();

        return View(new WorkshopListViewModel { Workshops = workshops });
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

        var workshop = new Workshop
        {
            Name = new LangStr(vm.Name),
            Address = new LangStr(vm.Address),
            Phone = vm.Phone,
            Email = vm.Email
        };

        _context.Workshops.Add(workshop);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var workshop = await _context.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        var vm = new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name.ToString(),
            Address = workshop.Address.ToString(),
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

        var workshop = await _context.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        workshop.Name = new LangStr(vm.Name);
        workshop.Address = new LangStr(vm.Address);
        workshop.Phone = vm.Phone;
        workshop.Email = vm.Email;
        workshop.UpdatedAt = DateTime.UtcNow;

        _context.Entry(workshop).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var workshop = await _context.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        var vm = new WorkshopViewModel
        {
            Id = workshop.Id,
            Name = workshop.Name.ToString(),
            Address = workshop.Address.ToString(),
            Phone = workshop.Phone,
            Email = workshop.Email
        };

        return View(vm);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var workshop = await _context.Workshops.FindAsync(id);
        if (workshop == null) return NotFound();

        _context.Workshops.Remove(workshop);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
