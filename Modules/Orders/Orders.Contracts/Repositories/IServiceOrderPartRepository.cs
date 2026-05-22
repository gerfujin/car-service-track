using Base.Contracts;
using Orders.Domain;

namespace Orders.Contracts.Repositories;

public interface IServiceOrderPartRepository : IBaseRepository<ServiceOrderPart>
{
    Task<IEnumerable<ServiceOrderPart>> AllWithDetailsAsync(Guid? serviceOrderId);
    Task<ServiceOrderPart?> FindWithDetailsAsync(Guid id);
    Task RecalculateOrderTotalAsync(Guid serviceOrderId);
}
