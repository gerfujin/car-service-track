using Base.Contracts;
using Orders.Domain;

namespace Orders.Contracts.Repositories;

public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<IEnumerable<Payment>> AllWithDetailsAsync(Guid? serviceOrderId = null);
    Task<IEnumerable<Payment>> AllByUserAsync(Guid appUserId, Guid? serviceOrderId = null);
    Task<Payment?> FindWithDetailsAsync(Guid id);
    Task<Payment?> FindWithDetailsForUserAsync(Guid id, Guid appUserId);
    Task<Payment?> FindByServiceOrderAsync(Guid serviceOrderId);
    Task<Payment?> FindByServiceOrderForUserAsync(Guid serviceOrderId, Guid appUserId);
    Task<bool> AnyByServiceOrderAsync(Guid serviceOrderId);
    Task<decimal> SumPaidAmountAsync();
}
