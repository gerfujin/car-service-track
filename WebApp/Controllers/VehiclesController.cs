using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels.Client;

namespace WebApp.Controllers;

[Authorize]
public class VehiclesController : Controller
{
    private readonly AppDbContext _context;

    public VehiclesController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private async Task<Owner?> GetCurrentOwnerAsync()
    {
        var userId = GetCurrentUserId();
        return await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /Vehicles
    public async Task<IActionResult> Index()
    {
        IQueryable<Vehicle> query = _context.Vehicles;

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null)
                return View(new VehicleListClientViewModel());
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicles = await query
            .Select(v => new VehicleClientViewModel
            {
                Id = v.Id,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                LicensePlate = v.LicensePlate,
                Vin = v.Vin,
                Mileage = v.Mileage,
                Color = v.Color,
                ServiceOrderCount = v.ServiceOrders != null ? v.ServiceOrders.Count : 0
            })
            .ToListAsync();

        ViewBag.IsReadOnly = IsMechanic();
        return View(new VehicleListClientViewModel { Vehicles = vehicles });
    }

    // GET: /Vehicles/Create — admin and client only
    [Authorize(Roles = "admin,client")]
    public IActionResult Create()
    {
        return View(new VehicleClientViewModel());
    }

    // POST: /Vehicles/Create — admin and client only
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> Create(VehicleClientViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId = GetCurrentUserId();
        var owner = await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);

        if (owner == null)
        {
            var user = await _context.Users.FindAsync(userId);
            owner = new Owner
            {
                AppUserId = userId,
                FirstName = user?.UserName?.Split('@')[0] ?? "User",
                LastName = ""
            };
            _context.Owners.Add(owner);
            await _context.SaveChangesAsync();
        }

        var vehicle = new Vehicle
        {
            Make = vm.Make,
            Model = vm.Model,
            Year = vm.Year,
            LicensePlate = vm.LicensePlate,
            Vin = vm.Vin,
            Mileage = vm.Mileage,
            Color = vm.Color,
            OwnerId = owner.Id
        };

        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Vehicles/Edit/{id} — admin and client only
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> Edit(Guid id)
    {
        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        var vm = new VehicleClientViewModel
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            Mileage = vehicle.Mileage,
            Color = vehicle.Color
        };

        return View(vm);
    }

    // POST: /Vehicles/Edit/{id} — admin and client only
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> Edit(Guid id, VehicleClientViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        vehicle.Make = vm.Make;
        vehicle.Model = vm.Model;
        vehicle.Year = vm.Year;
        vehicle.LicensePlate = vm.LicensePlate;
        vehicle.Vin = vm.Vin;
        vehicle.Mileage = vm.Mileage;
        vehicle.Color = vm.Color;
        vehicle.UpdatedAt = DateTime.UtcNow;

        _context.Entry(vehicle).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Vehicles/Details/{id}
    public async Task<IActionResult> Details(Guid id)
    {
        IQueryable<Vehicle> query = _context.Vehicles
            .Include(v => v.ServiceOrders)
            .Where(v => v.Id == id);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        var vm = new VehicleClientViewModel
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            Mileage = vehicle.Mileage,
            Color = vehicle.Color,
            ServiceOrderCount = vehicle.ServiceOrders?.Count ?? 0
        };

        ViewBag.IsReadOnly = IsMechanic();
        return View(vm);
    }

    // GET: /Vehicles/Delete/{id} — admin and client only
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> Delete(Guid id)
    {
        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        var vm = new VehicleClientViewModel
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Vin = vehicle.Vin,
            Mileage = vehicle.Mileage,
            Color = vehicle.Color
        };

        return View(vm);
    }

    // POST: /Vehicles/Delete/{id} — admin and client only
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        IQueryable<Vehicle> query = _context.Vehicles.Where(v => v.Id == id);

        if (!IsAdmin())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(v => v.OwnerId == owner.Id);
        }

        var vehicle = await query.FirstOrDefaultAsync();
        if (vehicle == null) return NotFound();

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
