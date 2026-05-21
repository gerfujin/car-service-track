using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;

namespace App.BLL.Services;

public class ListItemService : IListItemService
{
    private readonly IAppUnitOfWork _uow;

    public ListItemService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllListItem>> AllAsync()
    {
        var entities = await _uow.ListItems.AllAsync();
        return entities.Select(e => ListItemMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllListItem>> AllByUserAsync(Guid appUserId)
    {
        var entities = await _uow.ListItems.AllByUserAsync(appUserId);
        return entities.Select(e => ListItemMapper.ToBll(e)!).ToList();
    }

    public async Task<BllListItem?> FindAsync(Guid id)
    {
        return ListItemMapper.ToBll(await _uow.ListItems.FindAsync(id));
    }

    public async Task<BllListItem?> FindByUserAsync(Guid id, Guid appUserId)
    {
        return ListItemMapper.ToBll(await _uow.ListItems.FindByUserAsync(id, appUserId));
    }

    // Staging only — no SaveChanges here.
    public BllListItem Add(BllListItem entity)
    {
        var added = _uow.ListItems.Add(ListItemMapper.ToDomain(entity)!);
        return ListItemMapper.ToBll(added)!;
    }

    // Sync IBaseService.Update delegates to UpdateAsync (blocking) — one code path.
    public BllListItem Update(BllListItem entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here. Load-then-merge preserves CreatedAt.
    public async Task<BllListItem?> UpdateAsync(BllListItem entity)
    {
        var existing = await _uow.ListItems.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.ItemDescription = entity.ItemDescription;
        existing.IsDone = entity.IsDone;
        existing.Summary.SetTranslation(entity.Summary);

        _uow.ListItems.Update(existing);
        return ListItemMapper.ToBll(existing);
    }

    // Staging only — no SaveChanges here.
    public void Remove(BllListItem entity)
    {
        _uow.ListItems.Remove(ListItemMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.ListItems.ExistsAsync(id);
    }
}
