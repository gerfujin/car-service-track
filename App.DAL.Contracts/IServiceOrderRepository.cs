using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IServiceOrderRepository : IBaseRepository<ServiceOrder>
{
    public Task<IEnumerable<ServiceOrder>> AllByUserAsync(Guid appUserId);
    public Task<IEnumerable<ServiceOrder>> AllWithDetailsAsync();
    public Task<ServiceOrder?> FindWithDetailsAsync(Guid id);
    public Task<bool> AnyByVehicleAsync(Guid vehicleId);
    public Task<bool> AnyByWorkshopAsync(Guid workshopId);
    public Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId);

    /// <summary>
    /// Synchronises the ServiceOrderItems of an order to match the selected service ids:
    /// removes de-selected items, adds newly-selected ones (priced from Service.BasePrice),
    /// and leaves unchanged items intact. Stages only — caller persists via SaveChangesAsync.
    /// </summary>
    public Task UpdateServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> selectedServiceIds);

    /// <summary>
    /// Add-only variant for a brand-new order: looks up each service by id and stages a
    /// ServiceOrderItem with Quantity=1 and UnitPrice=Service.BasePrice. No diff logic —
    /// only call this on a freshly staged ServiceOrder with no existing items.
    /// Stages only — caller persists via SaveChangesAsync.
    /// </summary>
    public Task AddServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> serviceIds);
}
