using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IStatusHistoryService : IBaseService<BllStatusHistory>
{
    Task<IEnumerable<BllStatusHistory>> AllByOrderAsync(Guid serviceOrderId);
}
