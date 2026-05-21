using Microsoft.EntityFrameworkCore;
using Users.Contracts.Repositories;
using Users.Domain.Identity;

namespace Users.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<AppRefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(UsersDbContext context) : base(context)
    {
    }

    public async Task<int?> RemoveExpiredForUserAsync(Guid appUserId, DateTime now)
    {
        if (_context.Database.ProviderName!.Contains("InMemory"))
        {
            return null;
        }

        return await _context.RefreshTokens
            .Where(t => t.AppUserId == appUserId && t.ExpirationDT < now)
            .ExecuteDeleteAsync();
    }

    public async Task<List<AppRefreshToken>> FindValidForRefreshAsync(Guid appUserId, string refreshToken, DateTime now)
    {
        return await _context.RefreshTokens
            .Where(x => x.AppUserId == appUserId &&
                        x.RefreshToken == refreshToken &&
                        x.ExpirationDT > now)
            .ToListAsync();
    }

    public async Task RemoveForLogoutAsync(Guid appUserId, string refreshToken)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.AppUserId == appUserId &&
                        (x.RefreshToken == refreshToken ||
                         x.PreviousRefreshToken == refreshToken))
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(tokens);
    }
}
