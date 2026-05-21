using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;

namespace App.BLL.Services;

public class VehicleService : IVehicleService
{
    private readonly IAppUnitOfWork _uow;

    public VehicleService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllVehicle>> AllAsync()
    {
        var entities = await _uow.Vehicles.AllAsync();
        return entities.Select(e => VehicleMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllVehicle>> AllByUserAsync(Guid appUserId)
    {
        var entities = await _uow.Vehicles.AllByUserAsync(appUserId);
        return entities.Select(e => VehicleMapper.ToBll(e)!).ToList();
    }

    public async Task<BllVehicle?> FindAsync(Guid id)
    {
        return VehicleMapper.ToBll(await _uow.Vehicles.FindAsync(id));
    }

    public async Task<BllVehicle?> FindByUserAsync(Guid id, Guid appUserId)
    {
        return VehicleMapper.ToBll(await _uow.Vehicles.FindByUserAsync(id, appUserId));
    }

    // Staging only — no SaveChanges here (that is IAppBll.SaveChangesAsync's job).
    public BllVehicle Add(BllVehicle entity)
    {
        var added = _uow.Vehicles.Add(VehicleMapper.ToDomain(entity)!);
        return VehicleMapper.ToBll(added)!;
    }

    // Staging only — no SaveChanges here. The synchronous IBaseService.Update delegates to
    // UpdateAsync (blocking) so the IBaseService<T> contract is satisfied with one code path.
    public BllVehicle Update(BllVehicle entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here.
    // Load-then-merge: mutate the EXISTING entity so DB-managed audit fields (CreatedAt)
    // survive. Mapping a brand-new entity from the DTO would reset CreatedAt to now.
    // Returns null when the vehicle does not exist (consistent with ServiceOrderService).
    public async Task<BllVehicle?> UpdateAsync(BllVehicle entity)
    {
        var existing = await _uow.Vehicles.FindAsync(entity.Id);
        if (existing == null) return null;

        // Copy only the editable scalar fields — NOT Id, NOT OwnerId, NOT CreatedAt.
        existing.Make = entity.Make;
        existing.Model = entity.Model;
        existing.Year = entity.Year;
        existing.LicensePlate = entity.LicensePlate;
        existing.Vin = entity.Vin;
        existing.Mileage = entity.Mileage;
        existing.Color = entity.Color;
        existing.UpdatedAt = DateTime.UtcNow; // manual stamp — no SaveChanges override exists

        var updated = _uow.Vehicles.Update(existing);
        return VehicleMapper.ToBll(updated)!;
    }

    // Staging only — no SaveChanges here. Caller should check CanDeleteAsync first.
    public void Remove(BllVehicle entity)
    {
        _uow.Vehicles.Remove(VehicleMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Vehicles.ExistsAsync(id);
    }

    public async Task<bool> CanDeleteAsync(Guid vehicleId)
    {
        var hasOrders = await _uow.ServiceOrders.AnyByVehicleAsync(vehicleId);
        return !hasOrders;
    }
}
