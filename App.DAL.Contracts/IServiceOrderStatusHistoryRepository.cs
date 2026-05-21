using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IServiceOrderStatusHistoryRepository : IBaseRepository<ServiceOrderStatusHistory>
{
    Task<IEnumerable<ServiceOrderStatusHistory>> AllByOrderAsync(Guid serviceOrderId);
}
