using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IPaymentService : IBaseService<BllPayment>
{
    Task<IEnumerable<BllPayment>> AllWithDetailsAsync(Guid? serviceOrderId = null);
    Task<IEnumerable<BllPayment>> AllByUserAsync(Guid appUserId, Guid? serviceOrderId = null);
    Task<BllPayment?> FindWithDetailsAsync(Guid id);
    Task<BllPayment?> FindWithDetailsForUserAsync(Guid id, Guid appUserId);
    Task<BllPayment?> FindByServiceOrderAsync(Guid serviceOrderId);
    Task<BllPayment?> FindByServiceOrderForUserAsync(Guid serviceOrderId, Guid appUserId);
    Task<BllPayment?> UpdateAsync(BllPayment entity);

    /// <summary>
    /// Validates ServiceOrder existence and uniqueness, stages a new Pending payment, and returns
    /// the BllPayment populated with navigation data (OwnerId, VehicleInfo, WorkshopName).
    /// Returns (null, errorMessage) on validation failure. Staging only — caller persists via SaveChangesAsync.
    /// </summary>
    Task<(BllPayment? payment, string? error)> CreateForServiceOrderAsync(Guid serviceOrderId, decimal amount);

    /// <summary>
    /// Stages Status=Paid, PaidAt=UtcNow, UpdatedAt=UtcNow on the payment. Staging only.
    /// Caller is responsible for ensuring the payment exists and is in Pending status.
    /// </summary>
    Task MarkAsPaidAsync(Guid id);
}
