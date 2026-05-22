using Base.Contracts;
using Orders.Domain;

namespace Orders.Contracts.Repositories;

public interface IRepairPhotoRepository : IBaseRepository<RepairPhoto>
{
    Task<IEnumerable<RepairPhoto>> AllByServiceOrderWithDetailsAsync(Guid serviceOrderId);
    Task<RepairPhoto?> FindWithDetailsAsync(Guid id);
    Task<int> CountByServiceOrderAsync(Guid serviceOrderId);
}
