using App.BLL.DTO;
using App.Domain.Enums;
using Base.Contracts;

namespace App.BLL.Services;

public interface IServiceOrderService : IBaseService<BllServiceOrder>
{
    public Task<IEnumerable<BllServiceOrder>> AllByUserAsync(Guid appUserId);
    public Task<IEnumerable<BllServiceOrder>> AllWithDetailsAsync();
    public Task<BllServiceOrder?> FindWithDetailsAsync(Guid id);
    public Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId);

    /// <summary>
    /// Full async update: scalar load-then-merge (preserves CreatedAt) plus diffing of the
    /// selected services into ServiceOrderItems (add/remove, keep unchanged). Staging only.
    /// The synchronous <see cref="IBaseService{T}.Update"/> delegates to this.
    /// </summary>
    public Task<BllServiceOrder?> UpdateAsync(BllServiceOrder entity);

    /// <summary>
    /// Atomically stages a Status change on the order and a new StatusHistory entry.
    /// Sets CompletedDate when status is Completed. Staging only — caller persists via SaveChangesAsync.
    /// Returns false when the order does not exist.
    /// </summary>
    public Task<bool> SetStatusAsync(Guid orderId, ServiceOrderStatus status, string? notes);

    /// <summary>
    /// Stages the ServiceOrder and then stages each ServiceOrderItem priced from
    /// Service.BasePrice (DB lookup). Use this for the Create endpoint instead of the
    /// synchronous Add, which would create zero-priced items via the mapper.
    /// Staging only — caller persists via SaveChangesAsync.
    /// </summary>
    public Task<BllServiceOrder> AddWithItemsAsync(BllServiceOrder entity);

    /// <summary>
    /// Returns a list of service orders for dropdown rendering (admin).
    /// </summary>
    public Task<List<BllSelectListItem>> GetSelectListForAdminAsync();

    /// <summary>
    /// Admin status update that also sets MechanicId and FinalPrice in one operation.
    /// Staging only — caller persists via SaveChangesAsync.
    /// </summary>
    public Task<bool> UpdateStatusForAdminAsync(Guid orderId, ServiceOrderStatus status, Guid? mechanicId, decimal? finalPrice, string? notes);

    /// <summary>
    /// Queries aggregated dashboard statistics across all entities. Admin only — no user filtering.
    /// </summary>
    public Task<BllDashboardStats> GetAdminDashboardStatsAsync();

    /// <summary>
    /// Queries dashboard statistics filtered to a specific mechanic (by their identity AppUserId).
    /// </summary>
    public Task<BllDashboardStats> GetMechanicDashboardStatsAsync(Guid appUserId);

    /// <summary>Returns all service orders assigned to a specific mechanic (by their AppUserId).</summary>
    public Task<IEnumerable<BllServiceOrder>> AllByMechanicAsync(Guid appUserId);

    /// <summary>
    /// Finds a service order scoped to a mechanic. Returns null if order not found or not assigned to this mechanic.
    /// </summary>
    public Task<BllServiceOrder?> FindByMechanicAsync(Guid orderId, Guid appUserId);

    /// <summary>
    /// Updates the status of a service order, scoped to the mechanic (validates mechanic assignment).
    /// Returns false if the order does not exist or is not assigned to this mechanic.
    /// Staging only — caller persists via SaveChangesAsync.
    /// </summary>
    public Task<bool> UpdateStatusByMechanicAsync(Guid orderId, Guid appUserId, ServiceOrderStatus newStatus, string? notes);
}
