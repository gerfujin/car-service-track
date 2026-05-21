using Base.Contracts;
using Workshops.Application.DTO;

namespace Workshops.Application.Services;

public interface IServiceService : IBaseService<BllService>
{
    Task<BllService?> UpdateAsync(BllService entity);
}
