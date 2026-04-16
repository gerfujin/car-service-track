using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Mechanic.ViewModels;

namespace WebApp.Areas.Mechanic.Controllers;

[Area("Mechanic")]
[Authorize(Roles = "mechanic")]
public class OrdersController : Controller
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Mechanic/Orders
    public async Task<IActionResult> Index()
    {
        var orders = await _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.Payment)
            .OrderByDescending(so => so.OrderDate)
            .Select(so => new MechanicOrderViewModel
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

        return View(new MechanicOrderListViewModel { Orders = orders });
    }

    // GET: /Mechanic/Orders/UpdateStatus/{id}
    public async Task<IActionResult> UpdateStatus(Guid id)
    {
        var order = await _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .FirstOrDefaultAsync(so => so.Id == id);

        if (order == null) return NotFound();

        var vm = new MechanicUpdateStatusViewModel
        {
            Id = order.Id,
            CurrentStatus = order.Status,
            VehicleDisplay = order.Vehicle != null ? $"{order.Vehicle.Make} {order.Vehicle.Model} ({order.Vehicle.LicensePlate})" : "N/A",
            WorkshopName = order.Workshop != null ? order.Workshop.Name.ToString() : "N/A",
            Description = order.Description
        };

        return View(vm);
    }

    // POST: /Mechanic/Orders/UpdateStatus/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, MechanicUpdateStatusViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null) return NotFound();

        var previousStatus = order.Status;
        order.Status = vm.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (vm.NewStatus == ServiceOrderStatus.Completed)
        {
            order.CompletedDate = DateTime.UtcNow;
        }

        var statusHistory = new ServiceOrderStatusHistory
        {
            ServiceOrderId = order.Id,
            Status = vm.NewStatus,
            Notes = string.IsNullOrWhiteSpace(vm.Notes)
                ? $"Status changed from {previousStatus} to {vm.NewStatus}"
                : vm.Notes,
            ChangedAt = DateTime.UtcNow
        };
        _context.ServiceOrderStatusHistories.Add(statusHistory);

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Order status updated to {vm.NewStatus}.";
        return RedirectToAction(nameof(Index));
    }
}
