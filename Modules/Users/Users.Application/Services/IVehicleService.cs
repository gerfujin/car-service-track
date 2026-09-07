using Base.Contracts;
using Users.Application.DTO;

namespace Users.Application.Services;

public interface IVehicleService : IBaseService<BllVehicle>
{
    Task<IEnumerable<BllVehicle>> AllByUserAsync(Guid appUserId);
    Task<BllVehicle?> FindByUserAsync(Guid id, Guid appUserId);
    Task<BllVehicle?> FindByOwnerAsync(Guid id, Guid ownerId);

    Task<Guid> GetOrCreateOwnerIdAsync(Guid appUserId);
    Task<BllVehicle?> UpdateAsync(BllVehicle entity);
}
