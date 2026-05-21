using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IRepairPhotoRepository : IBaseRepository<RepairPhoto>
{
    Task<IEnumerable<RepairPhoto>> AllByServiceOrderWithDetailsAsync(Guid serviceOrderId);
    Task<RepairPhoto?> FindWithDetailsAsync(Guid id);
    Task<int> CountByServiceOrderAsync(Guid serviceOrderId);
}
