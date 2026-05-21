using Workshops.Application.DTO;
using Workshops.Application.Mappers;
using Workshops.Contracts;

namespace Workshops.Application.Services;

public class MechanicService : IMechanicService
{
    private readonly IWorkshopsUnitOfWork _uow;

    public MechanicService(IWorkshopsUnitOfWork uow)
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

    public BllMechanic Add(BllMechanic entity)
    {
        var added = _uow.Mechanics.Add(MechanicMapper.ToDomain(entity)!);
        return MechanicMapper.ToBll(added)!;
    }

    public BllMechanic Update(BllMechanic entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllMechanic?> UpdateAsync(BllMechanic entity)
    {
        var existing = await _uow.Mechanics.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.FirstName = entity.FirstName;
        existing.LastName = entity.LastName;
        existing.Phone = entity.Phone;
        existing.Email = entity.Email;
        existing.Specialization = entity.Specialization;
        existing.AppUserId = entity.AppUserId;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = _uow.Mechanics.Update(existing);
        return MechanicMapper.ToBll(updated)!;
    }

    public void Remove(BllMechanic entity)
    {
        _uow.Mechanics.Remove(MechanicMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Mechanics.ExistsAsync(id);
    }
}
