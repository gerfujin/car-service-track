using App.DAL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ListItemRepository : BaseRepository<ListItem>, IListItemRepository
{
    public ListItemRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ListItem>> AllByUserAsync(Guid appUserId)
    {
        return await _context.ListItems
            .Include(l => l.AppUser)
            .Where(l => l.AppUserId == appUserId)
            .ToListAsync();
    }

    public async Task<ListItem?> FindByUserAsync(Guid id, Guid appUserId)
    {
        return await _context.ListItems
            .Include(l => l.AppUser)
            .FirstOrDefaultAsync(l => l.Id == id && l.AppUserId == appUserId);
    }
}
