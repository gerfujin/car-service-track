using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;
using Base.Domain;

namespace App.BLL.Services;

public class WorkshopService : IWorkshopService
{
    private readonly IAppUnitOfWork _uow;

    public WorkshopService(IAppUnitOfWork uow)
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

    // Staging only — no SaveChanges here.
    public BllWorkshop Add(BllWorkshop entity)
    {
        var added = _uow.Workshops.Add(WorkshopMapper.ToDomain(entity)!);
        return WorkshopMapper.ToBll(added)!;
    }

    // Sync IBaseService.Update delegates to UpdateAsync (blocking) — one code path.
    public BllWorkshop Update(BllWorkshop entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here.
    // Load-then-merge: mutate the EXISTING entity so CreatedAt survives; replace the
    // multilingual fields the same way the old controllers did (new LangStr(...)).
    // Returns null when the workshop does not exist.
    public async Task<BllWorkshop?> UpdateAsync(BllWorkshop entity)
    {
        var existing = await _uow.Workshops.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Name = new LangStr(entity.Name);
        existing.Address = new LangStr(entity.Address);
        existing.Phone = entity.Phone;
        existing.Email = entity.Email;
        existing.UpdatedAt = DateTime.UtcNow; // manual stamp — no SaveChanges override exists

        var updated = _uow.Workshops.Update(existing);
        return WorkshopMapper.ToBll(updated)!;
    }

    // Staging only — no SaveChanges here.
    public void Remove(BllWorkshop entity)
    {
        _uow.Workshops.Remove(WorkshopMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Workshops.ExistsAsync(id);
    }

    public async Task<bool> CanDeleteAsync(Guid workshopId)
    {
        var inUse = await _uow.ServiceOrders.AnyByWorkshopAsync(workshopId);
        return !inUse;
    }
}
