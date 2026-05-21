using Base.Contracts;
using Workshops.Application.DTO;

namespace Workshops.Application.Services;

public interface ISparePartService : IBaseService<BllSparePart>
{
    Task<BllSparePart?> UpdateAsync(BllSparePart entity);
}
