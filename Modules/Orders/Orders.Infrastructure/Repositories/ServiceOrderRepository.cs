using Microsoft.EntityFrameworkCore;
using Orders.Contracts.Repositories;
using Orders.Domain;
using Orders.Domain.Enums;

namespace Orders.Infrastructure.Repositories;

public class ServiceOrderRepository : BaseRepository<ServiceOrder>, IServiceOrderRepository
{
    public ServiceOrderRepository(OrdersDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ServiceOrder>> AllByUserAsync(Guid appUserId)
    {
        return await _context.ServiceOrders
            .Where(so => so.AppUserId == appUserId)
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.Payment)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceOrder>> AllWithDetailsAsync()
    {
        return await _context.ServiceOrders
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.StatusHistory)
            .Include(so => so.RepairPhotos)
            .Include(so => so.Payment)
            .ToListAsync();
    }

    public async Task<ServiceOrder?> FindWithDetailsAsync(Guid id)
    {
        return await _context.ServiceOrders
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .Include(so => so.StatusHistory)
            .Include(so => so.RepairPhotos)
            .Include(so => so.Payment)
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
            .AnyAsync(so => so.Id == id && so.AppUserId == appUserId);
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

        var toRemove = existingItems.Where(i => !selected.Contains(i.ServiceId)).ToList();
        if (toRemove.Count > 0)
        {
            _context.ServiceOrderItems.RemoveRange(toRemove);
        }

        var toAddIds = selected.Where(sid => !existingServiceIds.Contains(sid)).ToList();
        foreach (var serviceId in toAddIds)
        {
            _context.ServiceOrderItems.Add(new ServiceOrderItem
            {
                ServiceOrderId = serviceOrderId,
                ServiceId = serviceId,
                Quantity = 1,
                UnitPrice = 0
            });
        }
    }

    public Task AddServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> serviceIds)
    {
        if (serviceIds.Count == 0) return Task.CompletedTask;

        foreach (var serviceId in serviceIds)
        {
            _context.ServiceOrderItems.Add(new ServiceOrderItem
            {
                ServiceOrderId = serviceOrderId,
                ServiceId = serviceId,
                Quantity = 1,
                UnitPrice = 0
            });
        }

        return Task.CompletedTask;
    }
}
