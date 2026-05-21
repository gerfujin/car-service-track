using Base.Contracts;
using Workshops.Application.DTO;

namespace Workshops.Application.Services;

public interface IWorkshopService : IBaseService<BllWorkshop>
{
    Task<BllWorkshop?> UpdateAsync(BllWorkshop entity);
}
