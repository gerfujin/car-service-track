using App.DAL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class OwnerRepository : BaseRepository<Owner>, IOwnerRepository
{
    public OwnerRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Owner?> FindByUserAsync(Guid appUserId)
    {
        return await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == appUserId);
    }
}
