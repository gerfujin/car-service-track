using Base.Contracts;
using Users.Domain;

namespace Users.Contracts.Repositories;

public interface IVehicleRepository : IBaseRepository<Vehicle>
{
    Task<IEnumerable<Vehicle>> AllByUserAsync(Guid appUserId);
    Task<Vehicle?> FindByUserAsync(Guid id, Guid appUserId);
    Task<Vehicle?> FindByOwnerAsync(Guid id, Guid ownerId);
}
