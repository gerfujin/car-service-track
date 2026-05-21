using Base.Contracts;
using Users.Domain.Identity;

namespace Users.Contracts.Repositories;

public interface IAppUserRepository : IBaseRepository<AppUser>
{
    Task<AppUser?> FindWithOwnerAsync(Guid id);
}
