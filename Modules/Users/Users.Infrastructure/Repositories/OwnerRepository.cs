using Microsoft.EntityFrameworkCore;
using Users.Contracts.Repositories;
using Users.Domain;

namespace Users.Infrastructure.Repositories;

public class OwnerRepository : BaseRepository<Owner>, IOwnerRepository
{
    public OwnerRepository(UsersDbContext context) : base(context)
    {
    }

    public async Task<Owner?> FindByUserAsync(Guid appUserId)
    {
        return await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == appUserId);
    }
}
