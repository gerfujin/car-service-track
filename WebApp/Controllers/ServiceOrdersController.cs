using App.DAL.EF;
using App.Domain.Enums;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels.Client;

namespace WebApp.Controllers;

[Authorize]
public class ServiceOrdersController : Controller
{
    private readonly AppDbContext _context;

    public ServiceOrdersController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /ServiceOrders — all roles
    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        var query = _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.Payment)
            .AsQueryable();

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: own orders only
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
            if (owner == null)
                return View(new ServiceOrderClientListViewModel());
            query = query.Where(so => so.Vehicle!.OwnerId == owner.Id);
        }

        var orders = await query
            .OrderByDescending(so => so.OrderDate)
            .Select(so => new ServiceOrderClientViewModel
            {
                Id = so.Id,
                Description = so.Description,
                Status = so.Status,
                OrderDate = so.OrderDate,
                CompletedDate = so.CompletedDate,
                VehicleDisplay = so.Vehicle != null ? $"{so.Vehicle.Make} {so.Vehicle.Model} ({so.Vehicle.LicensePlate})" : "N/A",
                WorkshopName = so.Workshop != null ? so.Workshop.Name.ToString() : "N/A",
                MechanicName = so.Mechanic != null ? $"{so.Mechanic.FirstName} {so.Mechanic.LastName}" : null,
                TotalAmount = (so.ServiceOrderItems != null ? so.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice) : 0) +
                              (so.ServiceOrderParts != null ? so.ServiceOrderParts.Sum(p => p.Quantity * p.UnitPrice) : 0),
                HasPayment = so.Payment != null
            })
            .ToListAsync();

        ViewBag.CanUpdateStatus = IsAdmin() || IsMechanic();
        return View(new ServiceOrderClientListViewModel { Orders = orders });
    }
}
