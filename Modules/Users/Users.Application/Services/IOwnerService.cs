using Base.Contracts;
using Users.Application.DTO;

namespace Users.Application.Services;

public interface IOwnerService : IBaseService<BllOwner>
{
    Task<BllOwner?> FindByUserAsync(Guid appUserId);

    Task<BllOwner> EnsureForUserAsync(Guid appUserId, string fallbackUserName);
}
