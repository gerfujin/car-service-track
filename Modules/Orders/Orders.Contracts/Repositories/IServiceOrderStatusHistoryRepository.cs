using Base.Contracts;
using Orders.Domain;

namespace Orders.Contracts.Repositories;

public interface IServiceOrderStatusHistoryRepository : IBaseRepository<ServiceOrderStatusHistory>
{
    Task<IEnumerable<ServiceOrderStatusHistory>> AllByOrderAsync(Guid serviceOrderId);
}
