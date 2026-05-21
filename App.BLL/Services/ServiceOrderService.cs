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
}
