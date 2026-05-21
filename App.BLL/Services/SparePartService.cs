using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;
using Base.Domain;

namespace App.BLL.Services;

public class SparePartService : ISparePartService
{
    private readonly IAppUnitOfWork _uow;

    public SparePartService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllSparePart>> AllAsync()
    {
        var entities = await _uow.SpareParts.AllAsync();
        return entities.Select(e => SparePartMapper.ToBll(e)!).ToList();
    }

    public async Task<BllSparePart?> FindAsync(Guid id)
    {
        return SparePartMapper.ToBll(await _uow.SpareParts.FindAsync(id));
    }

    // Staging only — no SaveChanges here.
    public BllSparePart Add(BllSparePart entity)
    {
        var added = _uow.SpareParts.Add(SparePartMapper.ToDomain(entity)!);
        return SparePartMapper.ToBll(added)!;
    }

    // Sync IBaseService.Update delegates to UpdateAsync (blocking) — one code path.
    public BllSparePart Update(BllSparePart entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here.
    // Load-then-merge: mutate the EXISTING entity so CreatedAt survives; replace the
    // multilingual Name the same way the old controller did (new LangStr(...)).
    // Returns null when the spare part does not exist.
    public async Task<BllSparePart?> UpdateAsync(BllSparePart entity)
    {
        var existing = await _uow.SpareParts.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Name = new LangStr(entity.Name);
        existing.PartNumber = entity.PartNumber;
        existing.Country = entity.Country;
        existing.UnitPrice = entity.UnitPrice;
        existing.StockQuantity = entity.StockQuantity;
        existing.UpdatedAt = DateTime.UtcNow; // manual stamp — no SaveChanges override exists

        var updated = _uow.SpareParts.Update(existing);
        return SparePartMapper.ToBll(updated)!;
    }

    // Staging only — no SaveChanges here.
    public void Remove(BllSparePart entity)
    {
        _uow.SpareParts.Remove(SparePartMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.SpareParts.ExistsAsync(id);
    }
}
