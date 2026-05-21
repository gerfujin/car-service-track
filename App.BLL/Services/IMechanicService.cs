using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IMechanicService : IBaseService<BllMechanic>
{
    /// <summary>
    /// Load-then-merge update (preserves CreatedAt, stamps UpdatedAt). Returns null when the
    /// mechanic does not exist. The synchronous <see cref="IBaseService{T}.Update"/> delegates here.
    /// </summary>
    public Task<BllMechanic?> UpdateAsync(BllMechanic entity);
}
