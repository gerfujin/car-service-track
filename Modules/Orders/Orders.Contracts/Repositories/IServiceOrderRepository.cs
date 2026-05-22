using Base.Contracts;
using Orders.Domain;
using Orders.Domain.Enums;

namespace Orders.Contracts.Repositories;

public interface IServiceOrderRepository : IBaseRepository<ServiceOrder>
{
    Task<IEnumerable<ServiceOrder>> AllByUserAsync(Guid appUserId);
    Task<IEnumerable<ServiceOrder>> AllWithDetailsAsync();
    Task<ServiceOrder?> FindWithDetailsAsync(Guid id);
    Task<bool> AnyByVehicleAsync(Guid vehicleId);
    Task<bool> AnyByWorkshopAsync(Guid workshopId);
    Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId);
    Task<int> CountByStatusAsync(ServiceOrderStatus status);
    Task<int> CountByMechanicAndStatusAsync(Guid mechanicId, ServiceOrderStatus status);
    Task UpdateServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> selectedServiceIds);
    Task AddServiceItemsAsync(Guid serviceOrderId, ICollection<Guid> serviceIds);
}
