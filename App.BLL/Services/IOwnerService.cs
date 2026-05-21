using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IOwnerService : IBaseService<BllOwner>
{
    Task<BllOwner?> FindByUserAsync(Guid appUserId);

    /// <summary>
    /// Returns the Owner for the given AppUser. If none exists, stages a new Owner
    /// (FirstName = fallbackUserName, LastName = "") and returns it. Staging only —
    /// caller persists via SaveChangesAsync. The returned BllOwner.Id is valid immediately
    /// (BaseEntity assigns Guid.NewGuid() on construction).
    /// </summary>
    Task<BllOwner> EnsureForUserAsync(Guid appUserId, string fallbackUserName);
}
