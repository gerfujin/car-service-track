using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IServiceService : IBaseService<BllService>
{
    /// <summary>
    /// Load-then-merge update (preserves CreatedAt, stamps UpdatedAt). Returns null when the
    /// service does not exist. The synchronous <see cref="IBaseService{T}.Update"/> delegates here.
    /// </summary>
    public Task<BllService?> UpdateAsync(BllService entity);
}
