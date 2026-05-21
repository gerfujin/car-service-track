using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;

namespace App.BLL.Services;

public class MechanicService : IMechanicService
{
    private readonly IAppUnitOfWork _uow;

    public MechanicService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllMechanic>> AllAsync()
    {
        var entities = await _uow.Mechanics.AllAsync();
        return entities.Select(e => MechanicMapper.ToBll(e)!).ToList();
    }

    public async Task<BllMechanic?> FindAsync(Guid id)
    {
        return MechanicMapper.ToBll(await _uow.Mechanics.FindAsync(id));
    }

    // Staging only - no SaveChanges here.
    public BllMechanic Add(BllMechanic entity)
    {
        var added = _uow.Mechanics.Add(MechanicMapper.ToDomain(entity)!);
        return MechanicMapper.ToBll(added)!;
    }

    // Sync IBaseService.Update delegates to UpdateAsync (blocking) - one code path.
    public BllMechanic Update(BllMechanic entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only - no SaveChanges here.
    // Load-then-merge: mutate the existing entity so CreatedAt and relations survive.
    // Returns null when the mechanic does not exist.
    public async Task<BllMechanic?> UpdateAsync(BllMechanic entity)
    {
        var existing = await _uow.Mechanics.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.FirstName = entity.FirstName;
        existing.LastName = entity.LastName;
        existing.Phone = entity.Phone;
        existing.Email = entity.Email;
        existing.Specialization = entity.Specialization;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = _uow.Mechanics.Update(existing);
        return MechanicMapper.ToBll(updated)!;
    }

    // Staging only - no SaveChanges here.
    public void Remove(BllMechanic entity)
    {
        _uow.Mechanics.Remove(MechanicMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Mechanics.ExistsAsync(id);
    }

    public async Task<List<BllSelectListItem>> GetSelectListAsync()
    {
        var mechanics = await AllAsync();
        return mechanics
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .Select(m => new BllSelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.FirstName} {m.LastName}"
            })
            .ToList();
    }
}
