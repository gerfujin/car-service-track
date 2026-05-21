using Base.Domain;
using Workshops.Application.DTO;
using Workshops.Application.Mappers;
using Workshops.Contracts;

namespace Workshops.Application.Services;

public class ServiceService : IServiceService
{
    private readonly IWorkshopsUnitOfWork _uow;

    public ServiceService(IWorkshopsUnitOfWork uow)
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

    public BllService Add(BllService entity)
    {
        var added = _uow.Services.Add(ServiceMapper.ToDomain(entity)!);
        return ServiceMapper.ToBll(added)!;
    }

    public BllService Update(BllService entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllService?> UpdateAsync(BllService entity)
    {
        var existing = await _uow.Services.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Name = new LangStr(entity.Name);
        existing.Description = new LangStr(entity.Description ?? string.Empty);
        existing.BasePrice = entity.BasePrice;
        existing.EstimatedTimeMinutes = entity.EstimatedTimeMinutes;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = _uow.Services.Update(existing);
        return ServiceMapper.ToBll(updated)!;
    }

    public void Remove(BllService entity)
    {
        _uow.Services.Remove(ServiceMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Services.ExistsAsync(id);
    }
}
