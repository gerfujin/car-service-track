using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IWorkshopService : IBaseService<BllWorkshop>
{
    /// <summary>
    /// Load-then-merge update (preserves CreatedAt, stamps UpdatedAt). Returns null when the
    /// workshop does not exist. The synchronous <see cref="IBaseService{T}.Update"/> delegates here.
    /// </summary>
    public Task<BllWorkshop?> UpdateAsync(BllWorkshop entity);

    /// <summary>
    /// Business rule: a workshop referenced by any service order cannot be deleted.
    /// Returns true when no service order references it (safe to delete).
    /// </summary>
    public Task<bool> CanDeleteAsync(Guid workshopId);
}
