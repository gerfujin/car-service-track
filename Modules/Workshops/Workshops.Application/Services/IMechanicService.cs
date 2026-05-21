using Base.Contracts;
using Workshops.Application.DTO;

namespace Workshops.Application.Services;

public interface IMechanicService : IBaseService<BllMechanic>
{
    Task<BllMechanic?> UpdateAsync(BllMechanic entity);
}
