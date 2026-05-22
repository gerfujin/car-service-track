using Users.Application.DTO;
using Users.Contracts;
using Users.Domain.Identity;

namespace Users.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IUsersUnitOfWork _uow;

    public RefreshTokenService(IUsersUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<int?> RemoveExpiredForUserAsync(Guid appUserId)
    {
        return await _uow.RefreshTokens.RemoveExpiredForUserAsync(appUserId, DateTime.UtcNow);
    }

    // Staging only — caller persists via SaveChangesAsync.
    public Task<string> AddForUserAsync(Guid appUserId)
    {
        var entity = _uow.RefreshTokens.Add(new AppRefreshToken { AppUserId = appUserId });
        return Task.FromResult(entity.RefreshToken);
    }

    // Staging only when rotating — caller persists via SaveChangesAsync when Rotated == true.
    public async Task<BllRefreshTokenRotationResult> RotateForRefreshAsync(Guid appUserId, string refreshToken)
    {
        var tokens = await _uow.RefreshTokens.FindValidForRefreshAsync(appUserId, refreshToken, DateTime.UtcNow);
        var result = new BllRefreshTokenRotationResult
        {
            MatchingTokenCount = tokens.Count,
            EmptyCollectionCountText = tokens.Count == 0 ? "" : tokens.Count.ToString()
        };

        if (tokens.Count != 1) return result;

        var token = tokens.First();
        if (token.RefreshToken == refreshToken)
        {
            token.PreviousRefreshToken = token.RefreshToken;
            token.PreviousExpirationDT = DateTime.UtcNow;
            token.RefreshToken = Guid.NewGuid().ToString();
            token.ExpirationDT = DateTime.UtcNow.AddDays(7);
            result.Rotated = true;
        }

        result.RefreshToken = token.RefreshToken;
        return result;
    }

    public async Task RemoveForLogoutAsync(Guid appUserId, string refreshToken)
    {
        await _uow.RefreshTokens.RemoveForLogoutAsync(appUserId, refreshToken);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _uow.SaveChangesAsync();
    }
}
