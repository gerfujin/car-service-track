using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin")]
public class ServiceOrderPartsController : Controller
{
    private readonly AppDbContext _context;

    public ServiceOrderPartsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(Guid? serviceOrderId)
    {
        var query = _context.ServiceOrderParts
            .Include(x => x.SparePart)
            .AsQueryable();

        if (serviceOrderId.HasValue)
        {
            query = query.Where(x => x.ServiceOrderId == serviceOrderId.Value);
        }

        var parts = await query
            .Select(x => new ServiceOrderPartAdminViewModel
            {
                Id = x.Id,
                ServiceOrderId = x.ServiceOrderId,
                SparePartId = x.SparePartId,
                Quantity = x.Quantity,
                Price = x.UnitPrice,
                SparePartName = x.SparePart != null ? (x.SparePart.Name.Translate() ?? x.SparePart.Name.ToString()) : null
            })
            .ToListAsync();

        return View(new ServiceOrderPartAdminListViewModel
        {
            ServiceOrderId = serviceOrderId,
            Parts = parts
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _context.ServiceOrderParts.Include(x => x.SparePart).FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();

        return View(new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice,
            SparePartName = entity.SparePart != null ? (entity.SparePart.Name.Translate() ?? entity.SparePart.Name.ToString()) : null
        });
    }

    public async Task<IActionResult> Create(Guid? serviceOrderId)
    {
        var vm = new ServiceOrderPartAdminViewModel
        {
            ServiceOrderId = serviceOrderId ?? Guid.Empty,
            Quantity = 1
        };
        await PopulateOptions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceOrderPartAdminViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptions(vm);
            return View(vm);
        }

        var order = await _context.ServiceOrders.FindAsync(vm.ServiceOrderId);
        var sparePart = await _context.SpareParts.FindAsync(vm.SparePartId);
        if (order == null || sparePart == null)
        {
            TempData["Error"] = "Service order or spare part not found.";
            await PopulateOptions(vm);
            return View(vm);
        }

        var entity = new ServiceOrderPart
        {
            ServiceOrderId = vm.ServiceOrderId,
            SparePartId = vm.SparePartId,
            Quantity = vm.Quantity,
            UnitPrice = vm.Price <= 0 ? sparePart.UnitPrice : vm.Price
        };

        _context.ServiceOrderParts.Add(entity);
        await _context.SaveChangesAsync();
        await RecalculateOrderTotalAsync(entity.ServiceOrderId);

        return RedirectToAction(nameof(Index), new { serviceOrderId = vm.ServiceOrderId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _context.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        var vm = new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice
        };

        await PopulateOptions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceOrderPartAdminViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateOptions(vm);
            return View(vm);
        }

        var entity = await _context.ServiceOrderParts.FindAsync(id);
        var sparePart = await _context.SpareParts.FindAsync(vm.SparePartId);
        if (entity == null || sparePart == null) return NotFound();

        entity.ServiceOrderId = vm.ServiceOrderId;
        entity.SparePartId = vm.SparePartId;
        entity.Quantity = vm.Quantity;
        entity.UnitPrice = vm.Price <= 0 ? sparePart.UnitPrice : vm.Price;

        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        await RecalculateOrderTotalAsync(entity.ServiceOrderId);

        return RedirectToAction(nameof(Index), new { serviceOrderId = vm.ServiceOrderId });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.ServiceOrderParts.Include(x => x.SparePart).FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();

        return View(new ServiceOrderPartAdminViewModel
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice,
            SparePartName = entity.SparePart != null ? (entity.SparePart.Name.Translate() ?? entity.SparePart.Name.ToString()) : null
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var entity = await _context.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        var orderId = entity.ServiceOrderId;
        _context.ServiceOrderParts.Remove(entity);
        await _context.SaveChangesAsync();
        await RecalculateOrderTotalAsync(orderId);

        return RedirectToAction(nameof(Index), new { serviceOrderId = orderId });
    }

    private async Task PopulateOptions(ServiceOrderPartAdminViewModel vm)
    {
        vm.ServiceOrderOptions = await _context.ServiceOrders
            .OrderByDescending(x => x.OrderDate)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.OrderDate:yyyy-MM-dd} | {x.Description ?? "Order"}"
            })
            .ToListAsync();

        vm.SparePartOptions = await _context.SpareParts
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{(x.Name.Translate() ?? x.Name.ToString())} (€{x.UnitPrice:F2})"
            })
            .ToListAsync();
    }

    private async Task RecalculateOrderTotalAsync(Guid serviceOrderId)
    {
        var order = await _context.ServiceOrders
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .FirstOrDefaultAsync(so => so.Id == serviceOrderId);

        if (order == null) return;
        var itemsTotal = order.ServiceOrderItems?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        var partsTotal = order.ServiceOrderParts?.Sum(p => p.Quantity * p.UnitPrice) ?? 0;
        order.FinalPrice = itemsTotal + partsTotal;
        order.UpdatedAt = DateTime.UtcNow;
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}

