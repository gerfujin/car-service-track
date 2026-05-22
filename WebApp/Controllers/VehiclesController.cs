using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Application.DTO;
using Users.Application.Services;
using Users.Contracts;
using WebApp.ViewModels.Client;

namespace WebApp.Controllers;

[Authorize]
public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly IUsersUnitOfWork _usersUow;

    public VehiclesController(IVehicleService vehicleService, IUsersUnitOfWork usersUow)
    {
        _vehicleService = vehicleService;
        _usersUow = usersUow;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /Vehicles
    public async Task<IActionResult> Index()
    {
        IEnumerable<BllVehicle> vehicles;

        if (IsAdmin() || IsMechanic())
        {
            vehicles = await _vehicleService.AllAsync();
        }
        else
        {
            var userId = GetCurrentUserId();
            vehicles = await _vehicleService.AllByUserAsync(userId);
        }

        var vm = new VehicleListClientViewModel
        {
            Vehicles = vehicles.Select(ToClientVm).ToList()
        };

        ViewBag.IsReadOnly = IsMechanic();
        return View(vm);
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
        var ownerId = await _vehicleService.GetOrCreateOwnerIdAsync(userId);

        var bllVehicle = new BllVehicle
        {
            Make = vm.Make,
            Model = vm.Model,
            Year = vm.Year,
            LicensePlate = vm.LicensePlate,
            Vin = vm.Vin,
            Mileage = vm.Mileage,
            Color = vm.Color,
            OwnerId = ownerId
        };

        _vehicleService.Add(bllVehicle);
        await _usersUow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Vehicles/Edit/{id} — admin and client only
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> Edit(Guid id)
    {
        BllVehicle? vehicle;

        if (IsAdmin())
        {
            vehicle = await _vehicleService.FindAsync(id);
        }
        else
        {
            var userId = GetCurrentUserId();
            vehicle = await _vehicleService.FindByUserAsync(id, userId);
        }

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

        BllVehicle? existing;

        if (IsAdmin())
        {
            existing = await _vehicleService.FindAsync(id);
        }
        else
        {
            var userId = GetCurrentUserId();
            existing = await _vehicleService.FindByUserAsync(id, userId);
        }

        if (existing == null) return NotFound();

        existing.Make = vm.Make;
        existing.Model = vm.Model;
        existing.Year = vm.Year;
        existing.LicensePlate = vm.LicensePlate;
        existing.Vin = vm.Vin;
        existing.Mileage = vm.Mileage;
        existing.Color = vm.Color;

        await _vehicleService.UpdateAsync(existing);
        await _usersUow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Vehicles/Details/{id}
    public async Task<IActionResult> Details(Guid id)
    {
        BllVehicle? vehicle;

        if (IsAdmin() || IsMechanic())
        {
            vehicle = await _vehicleService.FindAsync(id);
        }
        else
        {
            var userId = GetCurrentUserId();
            vehicle = await _vehicleService.FindByUserAsync(id, userId);
        }

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
            ServiceOrderCount = vehicle.ServiceOrderCount
        };

        ViewBag.IsReadOnly = IsMechanic();
        return View(vm);
    }

    // GET: /Vehicles/Delete/{id} — admin and client only
    [Authorize(Roles = "admin,client")]
    public async Task<IActionResult> Delete(Guid id)
    {
        BllVehicle? vehicle;

        if (IsAdmin())
        {
            vehicle = await _vehicleService.FindAsync(id);
        }
        else
        {
            var userId = GetCurrentUserId();
            vehicle = await _vehicleService.FindByUserAsync(id, userId);
        }

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
        BllVehicle? vehicle;

        if (IsAdmin())
        {
            vehicle = await _vehicleService.FindAsync(id);
        }
        else
        {
            var userId = GetCurrentUserId();
            vehicle = await _vehicleService.FindByUserAsync(id, userId);
        }

        if (vehicle == null) return NotFound();

        _vehicleService.Remove(vehicle);
        await _usersUow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ---- Mapping ----

    private static VehicleClientViewModel ToClientVm(BllVehicle vehicle) => new()
    {
        Id = vehicle.Id,
        Make = vehicle.Make,
        Model = vehicle.Model,
        Year = vehicle.Year,
        LicensePlate = vehicle.LicensePlate,
        Vin = vehicle.Vin,
        Mileage = vehicle.Mileage,
        Color = vehicle.Color,
        ServiceOrderCount = vehicle.ServiceOrderCount
    };
}
