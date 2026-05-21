using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;
using App.Domain;
using App.Domain.Enums;

namespace App.BLL.Services;

public class ServiceOrderService : IServiceOrderService
{
    private readonly IAppUnitOfWork _uow;

    public ServiceOrderService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllServiceOrder>> AllAsync()
    {
        var entities = await _uow.ServiceOrders.AllAsync();
        return entities.Select(e => ServiceOrderMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllServiceOrder>> AllByUserAsync(Guid appUserId)
    {
        var entities = await _uow.ServiceOrders.AllByUserAsync(appUserId);
        return entities.Select(e => ServiceOrderMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllServiceOrder>> AllWithDetailsAsync()
    {
        var entities = await _uow.ServiceOrders.AllWithDetailsAsync();
        return entities.Select(e => ServiceOrderMapper.ToBll(e)!).ToList();
    }

    public async Task<BllServiceOrder?> FindAsync(Guid id)
    {
        return ServiceOrderMapper.ToBll(await _uow.ServiceOrders.FindAsync(id));
    }

    public async Task<BllServiceOrder?> FindWithDetailsAsync(Guid id)
    {
        return ServiceOrderMapper.ToBll(await _uow.ServiceOrders.FindWithDetailsAsync(id));
    }

    // Staging only — no SaveChanges here. The selected service ids are turned into
    // ServiceOrderItem links by the mapper (logic moved out of the controller); the
    // repo stages the whole graph for the next SaveChangesAsync.
    public BllServiceOrder Add(BllServiceOrder entity)
    {
        var added = _uow.ServiceOrders.Add(ServiceOrderMapper.ToDomain(entity)!);
        return ServiceOrderMapper.ToBll(added)!;
    }

    // Staging only — no SaveChanges here. The synchronous IBaseService.Update delegates to
    // UpdateAsync (blocking) so all update logic — including item-diffing — lives in one place.
    public BllServiceOrder Update(BllServiceOrder entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here.
    // Load-then-merge so CreatedAt is preserved (we mutate the loaded entity, not a fresh one),
    // PLUS diff the selected services into ServiceOrderItems (add/remove, keep unchanged).
    public async Task<BllServiceOrder?> UpdateAsync(BllServiceOrder entity)
    {
        var existing = await _uow.ServiceOrders.FindAsync(entity.Id);
        if (existing == null) return null;

        // Merge editable scalar fields — NOT Id, NOT CreatedAt.
        existing.VehicleId = entity.VehicleId;
        existing.WorkshopId = entity.WorkshopId;
        existing.MechanicId = entity.MechanicId;
        existing.Description = entity.Description;
        existing.OrderDate = entity.OrderDate;
        existing.Status = entity.Status;
        existing.UpdatedAt = DateTime.UtcNow; // manual stamp — no SaveChanges override exists
        _uow.ServiceOrders.Update(existing);

        // Diff the child ServiceOrderItems to match the selected service ids. The DAL handles
        // add/remove + pricing from Service.BasePrice; unchanged items are left intact.
        await _uow.ServiceOrders.UpdateServiceItemsAsync(entity.Id, entity.ServiceIds);

        return ServiceOrderMapper.ToBll(existing);
    }

    // Staging only — no SaveChanges here.
    public void Remove(BllServiceOrder entity)
    {
        _uow.ServiceOrders.Remove(ServiceOrderMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.ServiceOrders.ExistsAsync(id);
    }

    public async Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId)
    {
        return await _uow.ServiceOrders.IsOwnedByUserAsync(id, appUserId);
    }

    public async Task<BllServiceOrder> AddWithItemsAsync(BllServiceOrder entity)
    {
        // Map to domain, but clear ServiceOrderItems: the mapper creates zero-priced items from
        // ServiceIds; pricing must come from the Service.BasePrice lookup in AddServiceItemsAsync.
        var domain = ServiceOrderMapper.ToDomain(entity)!;
        domain.ServiceOrderItems = new List<ServiceOrderItem>();

        var added = _uow.ServiceOrders.Add(domain);

        if (entity.ServiceIds.Any())
        {
            await _uow.ServiceOrders.AddServiceItemsAsync(added.Id, entity.ServiceIds);
        }

        return ServiceOrderMapper.ToBll(added)!;
    }

    public async Task<bool> SetStatusAsync(Guid orderId, ServiceOrderStatus status, string? notes)
    {
        var existing = await _uow.ServiceOrders.FindAsync(orderId);
        if (existing == null) return false;

        var previousStatus = existing.Status;
        existing.Status = status;
        existing.UpdatedAt = DateTime.UtcNow;

        if (status == ServiceOrderStatus.Completed)
        {
            existing.CompletedDate = DateTime.UtcNow;
        }

        _uow.ServiceOrders.Update(existing);

        _uow.StatusHistories.Add(new ServiceOrderStatusHistory
        {
            ServiceOrderId = orderId,
            Status = status,
            Notes = notes ?? $"Status changed from {previousStatus} to {status}",
            ChangedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<List<BllSelectListItem>> GetSelectListForAdminAsync()
    {
        var orders = await AllAsync();
        return orders
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new BllSelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{o.OrderDate:yyyy-MM-dd} | {o.Description ?? "Order"}"
            })
            .ToList();
    }

    public async Task<bool> UpdateStatusForAdminAsync(Guid orderId, ServiceOrderStatus status, Guid? mechanicId, decimal? finalPrice, string? notes)
    {
        var existing = await _uow.ServiceOrders.FindAsync(orderId);
        if (existing == null) return false;

        var previousStatus = existing.Status;
        existing.Status = status;
        existing.MechanicId = mechanicId;
        existing.FinalPrice = finalPrice;
        existing.UpdatedAt = DateTime.UtcNow;

        if (status == ServiceOrderStatus.Completed)
        {
            existing.CompletedDate = DateTime.UtcNow;
        }

        _uow.ServiceOrders.Update(existing);

        _uow.StatusHistories.Add(new ServiceOrderStatusHistory
        {
            ServiceOrderId = orderId,
            Status = status,
            Notes = notes ?? $"Status changed from {previousStatus} to {status}",
            ChangedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<BllDashboardStats> GetAdminDashboardStatsAsync()
    {
        var stats = new BllDashboardStats
        {
            TotalVehicles = await _uow.Vehicles.CountAsync(),
            TotalServiceOrders = await _uow.ServiceOrders.CountAsync(),
            TotalWorkshops = await _uow.Workshops.CountAsync(),
            TotalMechanics = await _uow.Mechanics.CountAsync(),
            TotalClients = await _uow.Owners.CountAsync(),
            PendingOrders = await _uow.ServiceOrders.CountByStatusAsync(ServiceOrderStatus.Pending),
            InProgressOrders = await _uow.ServiceOrders.CountByStatusAsync(ServiceOrderStatus.InProgress),
            CompletedOrders = await _uow.ServiceOrders.CountByStatusAsync(ServiceOrderStatus.Completed),
            TotalRevenue = await _uow.Payments.SumPaidAmountAsync(),
            TotalPayments = await _uow.Payments.CountAsync()
        };

        return stats;
    }

    public async Task<BllDashboardStats> GetMechanicDashboardStatsAsync(Guid appUserId)
    {
        var mechanics = await _uow.Mechanics.AllAsync();
        var mechanic = mechanics.FirstOrDefault(m => m.AppUserId == appUserId);
        var mechanicId = mechanic?.Id;

        var stats = new BllDashboardStats
        {
            TotalVehicles = await _uow.Vehicles.CountAsync(),
            TotalPayments = await _uow.Payments.CountAsync()
        };

        if (mechanicId.HasValue)
        {
            stats.PendingOrders = await _uow.ServiceOrders.CountByMechanicAndStatusAsync(mechanicId.Value, ServiceOrderStatus.Pending);
            stats.InProgressOrders = await _uow.ServiceOrders.CountByMechanicAndStatusAsync(mechanicId.Value, ServiceOrderStatus.InProgress);
            stats.CompletedOrders = await _uow.ServiceOrders.CountByMechanicAndStatusAsync(mechanicId.Value, ServiceOrderStatus.Completed);
            stats.TotalServiceOrders = stats.PendingOrders + stats.InProgressOrders + stats.CompletedOrders;
        }

        return stats;
    }

    public async Task<IEnumerable<BllServiceOrder>> AllByMechanicAsync(Guid appUserId)
    {
        var mechanics = await _uow.Mechanics.AllAsync();
        var mechanic = mechanics.FirstOrDefault(m => m.AppUserId == appUserId);
        if (mechanic == null) return Enumerable.Empty<BllServiceOrder>();

        var entities = await _uow.ServiceOrders.AllAsync();
        return entities
            .Where(so => so.MechanicId == mechanic.Id)
            .Select(so =>
            {
                var bll = ServiceOrderMapper.ToBll(so);
                // Populate read-side fields from loaded entities (AllAsync may not include navigation)
                if (bll != null)
                {
                    bll.VehicleDisplay = so.Vehicle != null ? $"{so.Vehicle.Make} {so.Vehicle.Model} ({so.Vehicle.LicensePlate})" : null;
                    bll.WorkshopName = so.Workshop?.Name.ToString();
                    bll.MechanicName = so.Mechanic != null ? $"{so.Mechanic.FirstName} {so.Mechanic.LastName}" : null;
                }
                return bll;
            })
            .Where(b => b != null)
            .Cast<BllServiceOrder>()
            .ToList();
    }

    public async Task<BllServiceOrder?> FindByMechanicAsync(Guid orderId, Guid appUserId)
    {
        var mechanics = await _uow.Mechanics.AllAsync();
        var mechanic = mechanics.FirstOrDefault(m => m.AppUserId == appUserId);
        if (mechanic == null) return null;

        var entity = await _uow.ServiceOrders.FindAsync(orderId);
        if (entity == null || entity.MechanicId != mechanic.Id) return null;

        var bll = ServiceOrderMapper.ToBll(entity);
        if (bll != null)
        {
            bll.VehicleDisplay = entity.Vehicle != null ? $"{entity.Vehicle.Make} {entity.Vehicle.Model} ({entity.Vehicle.LicensePlate})" : null;
            bll.WorkshopName = entity.Workshop?.Name.ToString();
            bll.MechanicName = entity.Mechanic != null ? $"{entity.Mechanic.FirstName} {entity.Mechanic.LastName}" : null;
        }
        return bll;
    }

    public async Task<bool> UpdateStatusByMechanicAsync(Guid orderId, Guid appUserId, ServiceOrderStatus newStatus, string? notes)
    {
        var mechanics = await _uow.Mechanics.AllAsync();
        var mechanic = mechanics.FirstOrDefault(m => m.AppUserId == appUserId);
        if (mechanic == null) return false;

        var entity = await _uow.ServiceOrders.FindAsync(orderId);
        if (entity == null || entity.MechanicId != mechanic.Id) return false;

        return await SetStatusAsync(orderId, newStatus, notes);
    }
}
