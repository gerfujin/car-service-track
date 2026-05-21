using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IVehicleRepository : IBaseRepository<Vehicle>
{
    public Task<IEnumerable<Vehicle>> AllByUserAsync(Guid appUserId);
    public Task<Vehicle?> FindByUserAsync(Guid id, Guid appUserId);
}
