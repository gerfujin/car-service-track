using Base.Contracts;
using Orders.Application.DTO;

namespace Orders.Application.Services;

public interface IStatusHistoryService : IBaseService<BllStatusHistory>
{
    Task<IEnumerable<BllStatusHistory>> AllByOrderAsync(Guid serviceOrderId);
}
