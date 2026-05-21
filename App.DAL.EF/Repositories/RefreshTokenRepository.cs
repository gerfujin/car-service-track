using App.DAL.Contracts;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class RefreshTokenRepository : BaseRepository<AppRefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<int?> RemoveExpiredForUserAsync(Guid appUserId, DateTime now)
    {
        // EF Core InMemory provider does not support ExecuteDeleteAsync, so skip during integration tests.
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
