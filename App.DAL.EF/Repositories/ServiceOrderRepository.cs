using App.DAL.Contracts;
using App.Domain;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ServiceOrderRepository : BaseRepository<ServiceOrder>, IServiceOrderRepository
{
    public ServiceOrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ServiceOrder>> AllByUserAsync(Guid appUserId)
    {
        return await _context.ServiceOrders
            .Where(so => so.Vehicle!.Owner!.AppUserId == appUserId)
            .Include(so => so.Vehicle)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems!)
                .ThenInclude(item => item.Service)
            .Include(so => so.ServiceOrderParts)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceOrder>> AllWithDetailsAsync()
    {
        return await _context.ServiceOrders
            .Include(so => so.Vehicle!)
                .ThenInclude(v => v!.Owner)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems!)
                .ThenInclude(item => item.Service)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.Payment)
            .ToListAsync();
    }

    public async Task<ServiceOrder?> FindWithDetailsAsync(Guid id)
    {
        return await _context.ServiceOrders
            .Include(so => so.Vehicle!)
                .ThenInclude(v => v.Owner)
            .Include(so => so.Workshop)
            .Include(so => so.Mechanic)
            .Include(so => so.ServiceOrderItems!)
                .ThenInclude(item => item.Service)
            .Include(so => so.ServiceOrderParts)
            .FirstOrDefaultAsync(so => so.Id == id);
    }

    public async Task<bool> AnyByVehicleAsync(Guid vehicleId)
    {
        return await _context.ServiceOrders.AnyAsync(so => so.VehicleId == vehicleId);
    }

    public async Task<bool> AnyByWorkshopAsync(Guid workshopId)
    {
        return await _context.ServiceOrders.AnyAsync(so => so.WorkshopId == workshopId);
    }

    public async Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId)
    {
        return await _context.ServiceOrders
            .AnyAsync(so => so.Id == id && so.Vehicle!.Owner!.AppUserId == appUserId);
    }

    public async Task<int> CountByStatusAsync(ServiceOrderStatus status)
    {
        return await _context.ServiceOrders.CountAsync(so => so.Status == status);
    }

    public async Task<int> CountByMechanicAndStatusAsync(Guid mechanicId, ServiceOrderStatus status)
    {
        return await _context.ServiceOrders.CountAsync(so => so.MechanicId == mechanicId && so.Status == status);
    }

    public async Task UpdateServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> selectedServiceIds)
    {
        var existingItems = await _context.ServiceOrderItems
            .Where(i => i.ServiceOrderId == serviceOrderId)
            .ToListAsync();

        var selected = selectedServiceIds.ToHashSet();
        var existingServiceIds = existingItems.Select(i => i.ServiceId).ToHashSet();

        // Remove items whose service is no longer selected.
        var toRemove = existingItems.Where(i => !selected.Contains(i.ServiceId)).ToList();
        if (toRemove.Count > 0)
        {
            _context.ServiceOrderItems.RemoveRange(toRemove);
        }

        // Add newly-selected services as items, priced from Service.BasePrice (same as create).
        var toAddIds = selected.Where(sid => !existingServiceIds.Contains(sid)).ToList();
        if (toAddIds.Count > 0)
        {
            var services = await _context.Services
                .Where(s => toAddIds.Contains(s.Id))
                .ToListAsync();

            foreach (var service in services)
            {
                _context.ServiceOrderItems.Add(new ServiceOrderItem
                {
                    ServiceOrderId = serviceOrderId,
                    ServiceId = service.Id,
                    Quantity = 1,
                    UnitPrice = service.BasePrice
                });
            }
        }
        // Unchanged items are deliberately left intact (avoids the fresh-GUID full-replace bug).
    }

    public async Task AddServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> serviceIds)
    {
        if (serviceIds.Count == 0) return;

        var services = await _context.Services
            .Where(s => serviceIds.Contains(s.Id))
            .ToListAsync();

        foreach (var service in services)
        {
            _context.ServiceOrderItems.Add(new ServiceOrderItem
            {
                ServiceOrderId = serviceOrderId,
                ServiceId = service.Id,
                Quantity = 1,
                UnitPrice = service.BasePrice
            });
        }
    }
}
