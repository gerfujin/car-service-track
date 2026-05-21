using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IListItemRepository : IBaseRepository<ListItem>
{
    Task<IEnumerable<ListItem>> AllByUserAsync(Guid appUserId);
    Task<ListItem?> FindByUserAsync(Guid id, Guid appUserId);
}
