using Base.Contracts;
using Users.Domain;

namespace Users.Contracts.Repositories;

public interface IOwnerRepository : IBaseRepository<Owner>
{
    Task<Owner?> FindByUserAsync(Guid appUserId);
}
