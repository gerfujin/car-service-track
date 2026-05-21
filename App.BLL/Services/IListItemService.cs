using App.BLL.DTO;
using Base.Contracts;

namespace App.BLL.Services;

public interface IListItemService : IBaseService<BllListItem>
{
    Task<IEnumerable<BllListItem>> AllByUserAsync(Guid appUserId);
    Task<BllListItem?> FindByUserAsync(Guid id, Guid appUserId);
    Task<BllListItem?> UpdateAsync(BllListItem entity);
}
