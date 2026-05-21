using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;

namespace App.BLL.Services;

public class RepairPhotoService : IRepairPhotoService
{
    private readonly IAppUnitOfWork _uow;

    public RepairPhotoService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllRepairPhoto>> AllAsync()
    {
        var entities = await _uow.RepairPhotos.AllAsync();
        return entities.Select(e => RepairPhotoMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllRepairPhoto>> AllForApiAsync(Guid serviceOrderId, Guid appUserId, bool isAdmin, bool isMechanic)
    {
        var entities = await _uow.RepairPhotos.AllByServiceOrderWithDetailsAsync(serviceOrderId);
        var photos = entities.Select(e => RepairPhotoMapper.ToBll(e)!).ToList();

        if (isAdmin || isMechanic)
        {
            return photos;
        }

        var owner = await _uow.Owners.FindByUserAsync(appUserId);
        if (owner == null)
        {
            return new List<BllRepairPhoto>();
        }

        return photos.Where(p => p.OwnerId == owner.Id).ToList();
    }

    public async Task<BllRepairPhoto?> FindAsync(Guid id)
    {
        return RepairPhotoMapper.ToBll(await _uow.RepairPhotos.FindAsync(id));
    }

    public async Task<BllRepairPhoto?> FindForApiAsync(Guid id, Guid appUserId, bool isAdmin, bool isMechanic)
    {
        var photo = RepairPhotoMapper.ToBll(await _uow.RepairPhotos.FindWithDetailsAsync(id));
        if (photo == null) return null;

        if (isAdmin || isMechanic)
        {
            return photo;
        }

        var owner = await _uow.Owners.FindByUserAsync(appUserId);
        if (owner == null || photo.OwnerId != owner.Id)
        {
            return null;
        }

        return photo;
    }

    public async Task<BllRepairPhotoMutationResult> ValidateUploadAsync(Guid serviceOrderId)
    {
        var orderExists = await _uow.ServiceOrders.ExistsAsync(serviceOrderId);
        if (!orderExists)
        {
            return new BllRepairPhotoMutationResult { Error = "Service order not found." };
        }

        var photoCount = await _uow.RepairPhotos.CountByServiceOrderAsync(serviceOrderId);
        if (photoCount >= 20)
        {
            return new BllRepairPhotoMutationResult { Error = "Maximum 20 photos per order." };
        }

        return new BllRepairPhotoMutationResult();
    }

    // Staging only - no SaveChanges here.
    public BllRepairPhoto Add(BllRepairPhoto entity)
    {
        var added = _uow.RepairPhotos.Add(RepairPhotoMapper.ToDomain(entity)!);
        return RepairPhotoMapper.ToBll(added)!;
    }

    public BllRepairPhoto AddUploaded(BllRepairPhoto entity)
    {
        entity.UploadedAt = DateTime.UtcNow;
        return Add(entity);
    }

    public BllRepairPhoto Update(BllRepairPhoto entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllRepairPhoto?> UpdateAsync(BllRepairPhoto entity)
    {
        var existing = await _uow.RepairPhotos.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.FilePath = entity.FilePath;
        existing.Description = entity.Description;
        existing.UploadedAt = entity.UploadedAt;
        existing.ServiceOrderId = entity.ServiceOrderId;

        var updated = _uow.RepairPhotos.Update(existing);
        return RepairPhotoMapper.ToBll(updated);
    }

    // Staging only - no SaveChanges here.
    public void Remove(BllRepairPhoto entity)
    {
        _uow.RepairPhotos.Remove(RepairPhotoMapper.ToDomain(entity)!);
    }

    public async Task<BllRepairPhotoMutationResult> RemoveForApiAsync(Guid id)
    {
        var existing = await _uow.RepairPhotos.FindAsync(id);
        if (existing == null)
        {
            return new BllRepairPhotoMutationResult { NotFound = true };
        }

        _uow.RepairPhotos.Remove(existing);

        return new BllRepairPhotoMutationResult
        {
            Entity = RepairPhotoMapper.ToBll(existing),
        };
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.RepairPhotos.ExistsAsync(id);
    }
}
