using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IOwnerRepository : IBaseRepository<Owner>
{
    Task<Owner?> FindByUserAsync(Guid appUserId);
}
