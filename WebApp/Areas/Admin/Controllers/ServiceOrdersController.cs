using App.DAL.EF;
using App.Domain;
using App.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin,mechanic")]
public class ServiceOrdersController : Controller
{
    private readonly AppDbContext _context;

    public ServiceOrdersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.ServiceOrders
            .Include(so => so.Vehicle)
                .ThenInclude(v => v!.Owner)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.Payment)
            .OrderByDescending(so => so.OrderDate)
            .Select(so => new ServiceOrderAdminViewModel
            {
                Id = so.Id,
                Description = so.Description,
                Status = so.Status,
                OrderDate = so.OrderDate,
                CompletedDate = so.CompletedDate,
                VehicleDisplay = so.Vehicle != null ? $"{so.Vehicle.Make} {so.Vehicle.Model} ({so.Vehicle.LicensePlate})" : "N/A",
                OwnerName = so.Vehicle != null && so.Vehicle.Owner != null
                    ? $"{so.Vehicle.Owner.FirstName} {so.Vehicle.Owner.LastName}"
                    : "N/A",
                WorkshopName = so.Workshop != null ? so.Workshop.Name.ToString() : "N/A",
                MechanicName = so.Mechanic != null ? $"{so.Mechanic.FirstName} {so.Mechanic.LastName}" : null,
                TotalAmount = (so.ServiceOrderItems != null ? so.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice) : 0) +
                              (so.ServiceOrderParts != null ? so.ServiceOrderParts.Sum(p => p.Quantity * p.UnitPrice) : 0),
                HasPayment = so.Payment != null
            })
            .ToListAsync();

        return View(new ServiceOrderAdminListViewModel { Orders = orders });
    }

    public async Task<IActionResult> UpdateStatus(Guid id)
    {
        var order = await _context.ServiceOrders
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .FirstOrDefaultAsync(so => so.Id == id);

        if (order == null) return NotFound();

        var mechanics = await _context.Mechanics
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.FirstName} {m.LastName}"
            })
            .ToListAsync();

        var vm = new ServiceOrderStatusUpdateViewModel
        {
            Id = order.Id,
            CurrentStatus = order.Status,
            NewStatus = order.Status,
            MechanicId = order.MechanicId,
            StatusOptions = Enum.GetValues<ServiceOrderStatus>()
                .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList(),
            MechanicOptions = mechanics
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, ServiceOrderStatusUpdateViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        var order = await _context.ServiceOrders.FindAsync(id);
        if (order == null) return NotFound();

        var oldStatus = order.Status;
        order.Status = vm.NewStatus;
        order.MechanicId = vm.MechanicId;
        order.UpdatedAt = DateTime.UtcNow;

        if (vm.NewStatus == ServiceOrderStatus.Completed)
        {
            order.CompletedDate = DateTime.UtcNow;
        }

        // Add status history entry
        var history = new ServiceOrderStatusHistory
        {
            ServiceOrderId = order.Id,
            Status = vm.NewStatus,
            Notes = vm.Notes,
            ChangedAt = DateTime.UtcNow
        };
        _context.ServiceOrderStatusHistories.Add(history);

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
