using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class MechanicsController : Controller
{
    private readonly AppDbContext _context;

    public MechanicsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var mechanics = await _context.Mechanics
            .Select(m => new MechanicViewModel
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Phone = m.Phone,
                Email = m.Email,
                Specialization = m.Specialization
            })
            .ToListAsync();

        return View(new MechanicListViewModel { Mechanics = mechanics });
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

        var mechanic = new App.Domain.Mechanic
        {
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            Phone = vm.Phone,
            Email = vm.Email,
            Specialization = vm.Specialization
        };

        _context.Mechanics.Add(mechanic);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Mechanic {mechanic.FirstName} {mechanic.LastName} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var mechanic = await _context.Mechanics.FindAsync(id);
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

        var mechanic = await _context.Mechanics.FindAsync(id);
        if (mechanic == null) return NotFound();

        mechanic.FirstName = vm.FirstName;
        mechanic.LastName = vm.LastName;
        mechanic.Phone = vm.Phone;
        mechanic.Email = vm.Email;
        mechanic.Specialization = vm.Specialization;
        mechanic.UpdatedAt = DateTime.UtcNow;

        _context.Entry(mechanic).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Mechanic {mechanic.FirstName} {mechanic.LastName} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var mechanic = await _context.Mechanics.FindAsync(id);
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
        var mechanic = await _context.Mechanics.FindAsync(id);
        if (mechanic == null) return NotFound();

        _context.Mechanics.Remove(mechanic);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Mechanic deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
