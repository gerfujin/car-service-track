using Base.Domain;
using Workshops.Application.DTO;
using Workshops.Application.Mappers;
using Workshops.Contracts;

namespace Workshops.Application.Services;

public class SparePartService : ISparePartService
{
    private readonly IWorkshopsUnitOfWork _uow;

    public SparePartService(IWorkshopsUnitOfWork uow)
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

    public BllSparePart Add(BllSparePart entity)
    {
        var added = _uow.SpareParts.Add(SparePartMapper.ToDomain(entity)!);
        return SparePartMapper.ToBll(added)!;
    }

    public BllSparePart Update(BllSparePart entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllSparePart?> UpdateAsync(BllSparePart entity)
    {
        var existing = await _uow.SpareParts.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Name = new LangStr(entity.Name);
        existing.PartNumber = entity.PartNumber;
        existing.Country = entity.Country;
        existing.UnitPrice = entity.UnitPrice;
        existing.StockQuantity = entity.StockQuantity;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = _uow.SpareParts.Update(existing);
        return SparePartMapper.ToBll(updated)!;
    }

    public void Remove(BllSparePart entity)
    {
        _uow.SpareParts.Remove(SparePartMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.SpareParts.ExistsAsync(id);
    }
}
