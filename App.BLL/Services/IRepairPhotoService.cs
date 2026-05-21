using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IRepairPhotoService : IBaseService<BllRepairPhoto>
{
    Task<IEnumerable<BllRepairPhoto>> AllForApiAsync(Guid serviceOrderId, Guid appUserId, bool isAdmin, bool isMechanic);
    Task<BllRepairPhoto?> FindForApiAsync(Guid id, Guid appUserId, bool isAdmin, bool isMechanic);
    Task<BllRepairPhotoMutationResult> ValidateUploadAsync(Guid serviceOrderId);
    BllRepairPhoto AddUploaded(BllRepairPhoto entity);
    Task<BllRepairPhotoMutationResult> RemoveForApiAsync(Guid id);
    Task<BllRepairPhoto?> UpdateAsync(BllRepairPhoto entity);
}
