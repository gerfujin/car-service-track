using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface ISparePartService : IBaseService<BllSparePart>
{
    /// <summary>
    /// Load-then-merge update (preserves CreatedAt, stamps UpdatedAt). Returns null when the
    /// spare part does not exist. The synchronous <see cref="IBaseService{T}.Update"/> delegates here.
    /// </summary>
    public Task<BllSparePart?> UpdateAsync(BllSparePart entity);
}
