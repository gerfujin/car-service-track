using Base.Contracts;
using Orders.Application.DTO;
using Orders.Domain.Enums;

namespace Orders.Application.Services;

public interface IServiceOrderService : IBaseService<BllServiceOrder>
{
    Task<IEnumerable<BllServiceOrder>> AllByUserAsync(Guid appUserId);
    Task<IEnumerable<BllServiceOrder>> AllWithDetailsAsync();
    Task<BllServiceOrder?> FindWithDetailsAsync(Guid id);
    Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId);
    Task<BllServiceOrder?> UpdateAsync(BllServiceOrder entity);
    Task<bool> SetStatusAsync(Guid orderId, ServiceOrderStatus status, string? notes);
    Task<BllServiceOrder> AddWithItemsAsync(BllServiceOrder entity);
    Task<List<BllSelectListItem>> GetSelectListForAdminAsync();
    Task<bool> UpdateStatusForAdminAsync(Guid orderId, ServiceOrderStatus status, Guid? mechanicId, decimal? finalPrice, string? notes);
    Task<BllDashboardStats> GetAdminDashboardStatsAsync();
    Task<BllDashboardStats> GetMechanicDashboardStatsAsync(Guid appUserId);
    Task<IEnumerable<BllServiceOrder>> AllByMechanicAsync(Guid appUserId);
    Task<BllServiceOrder?> FindByMechanicAsync(Guid orderId, Guid appUserId);
    Task<bool> UpdateStatusByMechanicAsync(Guid orderId, Guid appUserId, ServiceOrderStatus newStatus, string? notes);
    Task<bool> HasOrdersForVehicleAsync(Guid vehicleId);
}
