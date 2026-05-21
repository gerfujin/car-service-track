using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IServiceOrderPartService : IBaseService<BllServiceOrderPart>
{
    Task<IEnumerable<BllServiceOrderPart>> AllForApiAsync(Guid? serviceOrderId, Guid appUserId, bool isAdmin, bool isMechanic);
    Task<BllServiceOrderPart?> FindForApiAsync(Guid id, Guid appUserId, bool isAdmin, bool isMechanic);
    Task<BllServiceOrderPartMutationResult> CreateForApiAsync(BllServiceOrderPart entity);
    Task<BllServiceOrderPartMutationResult> UpdateForApiAsync(Guid id, BllServiceOrderPart entity);
    Task<BllServiceOrderPartMutationResult> RemoveForApiAsync(Guid id);
    Task RecalculateOrderTotalAsync(Guid serviceOrderId);
    Task<BllServiceOrderPart?> UpdateAsync(BllServiceOrderPart entity);
}
