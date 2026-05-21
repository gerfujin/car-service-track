using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IVehicleService : IBaseService<BllVehicle>
{
    public Task<IEnumerable<BllVehicle>> AllByUserAsync(Guid appUserId);
    public Task<BllVehicle?> FindByUserAsync(Guid id, Guid appUserId);

    /// <summary>
    /// Full async update: load-then-merge (preserves CreatedAt). Returns null when the
    /// vehicle does not exist. The synchronous <see cref="IBaseService{T}.Update"/> delegates here.
    /// </summary>
    public Task<BllVehicle?> UpdateAsync(BllVehicle entity);

    /// <summary>
    /// Business rule: a vehicle that still has service orders may not be deleted.
    /// Returns true when the vehicle has no service orders and is safe to delete.
    /// </summary>
    public Task<bool> CanDeleteAsync(Guid vehicleId);
}
