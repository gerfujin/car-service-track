using Microsoft.EntityFrameworkCore;
using Users.Contracts.Repositories;
using Users.Domain.Identity;

namespace Users.Infrastructure.Repositories;

public class AppUserRepository : BaseRepository<AppUser>, IAppUserRepository
{
    public AppUserRepository(UsersDbContext context) : base(context)
    {
    }

    public async Task<AppUser?> FindWithOwnerAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.Owner)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
