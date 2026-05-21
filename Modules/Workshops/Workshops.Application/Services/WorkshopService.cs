using Base.Domain;
using Workshops.Application.DTO;
using Workshops.Application.Mappers;
using Workshops.Contracts;

namespace Workshops.Application.Services;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopsUnitOfWork _uow;

    public WorkshopService(IWorkshopsUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllWorkshop>> AllAsync()
    {
        var entities = await _uow.Workshops.AllAsync();
        return entities.Select(e => WorkshopMapper.ToBll(e)!).ToList();
    }

    public async Task<BllWorkshop?> FindAsync(Guid id)
    {
        return WorkshopMapper.ToBll(await _uow.Workshops.FindAsync(id));
    }

    public BllWorkshop Add(BllWorkshop entity)
    {
        var added = _uow.Workshops.Add(WorkshopMapper.ToDomain(entity)!);
        return WorkshopMapper.ToBll(added)!;
    }

    public BllWorkshop Update(BllWorkshop entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllWorkshop?> UpdateAsync(BllWorkshop entity)
    {
        var existing = await _uow.Workshops.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Name = new LangStr(entity.Name);
        existing.Address = new LangStr(entity.Address);
        existing.Phone = entity.Phone;
        existing.Email = entity.Email;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = _uow.Workshops.Update(existing);
        return WorkshopMapper.ToBll(updated)!;
    }

    public void Remove(BllWorkshop entity)
    {
        _uow.Workshops.Remove(WorkshopMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Workshops.ExistsAsync(id);
    }
}
