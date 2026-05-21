using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;
using Base.Domain;

namespace App.BLL.Services;

public class ServiceService : IServiceService
{
    private readonly IAppUnitOfWork _uow;

    public ServiceService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllService>> AllAsync()
    {
        var entities = await _uow.Services.AllAsync();
        return entities.Select(e => ServiceMapper.ToBll(e)!).ToList();
    }

    public async Task<BllService?> FindAsync(Guid id)
    {
        return ServiceMapper.ToBll(await _uow.Services.FindAsync(id));
    }

    // Staging only — no SaveChanges here.
    public BllService Add(BllService entity)
    {
        var added = _uow.Services.Add(ServiceMapper.ToDomain(entity)!);
        return ServiceMapper.ToBll(added)!;
    }

    // Sync IBaseService.Update delegates to UpdateAsync (blocking) — one code path.
    public BllService Update(BllService entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here.
    // Load-then-merge: mutate the EXISTING entity so CreatedAt survives; replace the
    // multilingual fields the same way the old controller did (new LangStr(...)).
    // Returns null when the service does not exist.
    public async Task<BllService?> UpdateAsync(BllService entity)
    {
        var existing = await _uow.Services.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Name = new LangStr(entity.Name);
        existing.Description = new LangStr(entity.Description ?? string.Empty);
        existing.BasePrice = entity.BasePrice;
        existing.EstimatedTimeMinutes = entity.EstimatedTimeMinutes;
        existing.UpdatedAt = DateTime.UtcNow; // manual stamp — no SaveChanges override exists

        var updated = _uow.Services.Update(existing);
        return ServiceMapper.ToBll(updated)!;
    }

    // Staging only — no SaveChanges here.
    public void Remove(BllService entity)
    {
        _uow.Services.Remove(ServiceMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Services.ExistsAsync(id);
    }
}
