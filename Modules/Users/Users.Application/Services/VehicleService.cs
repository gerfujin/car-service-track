using Users.Application.DTO;
using Users.Application.Mappers;
using Users.Contracts;
using Users.Domain;

namespace Users.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IUsersUnitOfWork _uow;

    public VehicleService(IUsersUnitOfWork uow)
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

    public async Task<BllVehicle?> FindByOwnerAsync(Guid id, Guid ownerId)
    {
        return VehicleMapper.ToBll(await _uow.Vehicles.FindByOwnerAsync(id, ownerId));
    }

    public BllVehicle Add(BllVehicle entity)
    {
        var added = _uow.Vehicles.Add(VehicleMapper.ToDomain(entity)!);
        return VehicleMapper.ToBll(added)!;
    }

    public BllVehicle Update(BllVehicle entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllVehicle?> UpdateAsync(BllVehicle entity)
    {
        var existing = await _uow.Vehicles.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Make = entity.Make;
        existing.Model = entity.Model;
        existing.Year = entity.Year;
        existing.LicensePlate = entity.LicensePlate;
        existing.Vin = entity.Vin;
        existing.Mileage = entity.Mileage;
        existing.Color = entity.Color;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = _uow.Vehicles.Update(existing);
        return VehicleMapper.ToBll(updated)!;
    }

    public void Remove(BllVehicle entity)
    {
        _uow.Vehicles.Remove(VehicleMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Vehicles.ExistsAsync(id);
    }

    public async Task<Guid> GetOrCreateOwnerIdAsync(Guid appUserId)
    {
        var owner = await _uow.Owners.FindByUserAsync(appUserId);
        if (owner != null) return owner.Id;

        var newOwner = new Owner
        {
            AppUserId = appUserId,
            FirstName = "User",
            LastName = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var added = _uow.Owners.Add(newOwner);
        await _uow.SaveChangesAsync();
        return added.Id;
    }
}
