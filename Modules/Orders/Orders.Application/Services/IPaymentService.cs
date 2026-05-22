using Base.Contracts;
using Orders.Application.DTO;

namespace Orders.Application.Services;

public interface IPaymentService : IBaseService<BllPayment>
{
    Task<IEnumerable<BllPayment>> AllWithDetailsAsync(Guid? serviceOrderId = null);
    Task<IEnumerable<BllPayment>> AllByUserAsync(Guid appUserId, Guid? serviceOrderId = null);
    Task<BllPayment?> FindWithDetailsAsync(Guid id);
    Task<BllPayment?> FindWithDetailsForUserAsync(Guid id, Guid appUserId);
    Task<BllPayment?> FindByServiceOrderAsync(Guid serviceOrderId);
    Task<BllPayment?> FindByServiceOrderForUserAsync(Guid serviceOrderId, Guid appUserId);
    Task<BllPayment?> UpdateAsync(BllPayment entity);
    Task<(BllPayment? payment, string? error)> CreateForServiceOrderAsync(Guid serviceOrderId, decimal amount);
    Task MarkAsPaidAsync(Guid id);
}
