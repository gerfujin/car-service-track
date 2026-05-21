using Users.Application.DTO;
using Users.Application.Mappers;
using Users.Contracts;
using Users.Domain;

namespace Users.Application.Services;

public class OwnerService : IOwnerService
{
    private readonly IUsersUnitOfWork _uow;

    public OwnerService(IUsersUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllOwner>> AllAsync()
    {
        var entities = await _uow.Owners.AllAsync();
        return entities.Select(e => OwnerMapper.ToBll(e)!).ToList();
    }

    public async Task<BllOwner?> FindAsync(Guid id)
    {
        return OwnerMapper.ToBll(await _uow.Owners.FindAsync(id));
    }

    public async Task<BllOwner?> FindByUserAsync(Guid appUserId)
    {
        return OwnerMapper.ToBll(await _uow.Owners.FindByUserAsync(appUserId));
    }

    public BllOwner Add(BllOwner entity)
    {
        var added = _uow.Owners.Add(OwnerMapper.ToDomain(entity)!);
        return OwnerMapper.ToBll(added)!;
    }

    public BllOwner Update(BllOwner entity)
    {
        var existing = _uow.Owners.FindAsync(entity.Id).GetAwaiter().GetResult();
        if (existing == null) return entity;
        existing.FirstName = entity.FirstName;
        existing.LastName = entity.LastName;
        existing.Address = entity.Address;
        existing.Phone = entity.Phone;
        existing.UpdatedAt = DateTime.UtcNow;
        var updated = _uow.Owners.Update(existing);
        return OwnerMapper.ToBll(updated)!;
    }

    public void Remove(BllOwner entity)
    {
        _uow.Owners.Remove(OwnerMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Owners.ExistsAsync(id);
    }

    public async Task<BllOwner> EnsureForUserAsync(Guid appUserId, string fallbackUserName)
    {
        var existing = await _uow.Owners.FindByUserAsync(appUserId);
        if (existing != null)
            return OwnerMapper.ToBll(existing)!;

        var domain = new Owner
        {
            AppUserId = appUserId,
            FirstName = fallbackUserName,
            LastName = ""
        };
        var added = _uow.Owners.Add(domain);
        return OwnerMapper.ToBll(added)!;
    }
}
