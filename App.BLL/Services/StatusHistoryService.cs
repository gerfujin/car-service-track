using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;

namespace App.BLL.Services;

public class StatusHistoryService : IStatusHistoryService
{
    private readonly IAppUnitOfWork _uow;

    public StatusHistoryService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllStatusHistory>> AllAsync()
    {
        var entities = await _uow.StatusHistories.AllAsync();
        return entities.Select(e => StatusHistoryMapper.ToBll(e)!).ToList();
    }

    public async Task<BllStatusHistory?> FindAsync(Guid id)
    {
        return StatusHistoryMapper.ToBll(await _uow.StatusHistories.FindAsync(id));
    }

    public BllStatusHistory Add(BllStatusHistory entity)
    {
        var added = _uow.StatusHistories.Add(StatusHistoryMapper.ToDomain(entity)!);
        return StatusHistoryMapper.ToBll(added)!;
    }

    public BllStatusHistory Update(BllStatusHistory entity)
    {
        var updated = _uow.StatusHistories.Update(StatusHistoryMapper.ToDomain(entity)!);
        return StatusHistoryMapper.ToBll(updated)!;
    }

    public void Remove(BllStatusHistory entity)
    {
        _uow.StatusHistories.Remove(StatusHistoryMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.StatusHistories.ExistsAsync(id);
    }

    public async Task<IEnumerable<BllStatusHistory>> AllByOrderAsync(Guid serviceOrderId)
    {
        var entities = await _uow.StatusHistories.AllByOrderAsync(serviceOrderId);
        return entities.Select(e => StatusHistoryMapper.ToBll(e)!).ToList();
    }
}
