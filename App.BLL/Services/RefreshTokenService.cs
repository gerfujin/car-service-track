using App.BLL.DTO;
using App.DAL.Contracts;
using App.Domain.Identity;

namespace App.BLL.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IAppUnitOfWork _uow;

    public RefreshTokenService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<int?> RemoveExpiredForUserAsync(Guid appUserId)
    {
        return await _uow.RefreshTokens.RemoveExpiredForUserAsync(appUserId, DateTime.UtcNow);
    }

    // Staging only - no SaveChanges here.
    public string AddForUser(Guid appUserId)
    {
        var refreshToken = _uow.RefreshTokens.Add(new AppRefreshToken
        {
            AppUserId = appUserId
        });

        return refreshToken.RefreshToken;
    }

    // Staging only when the current token is rotated - caller persists via SaveChangesAsync.
    public async Task<BllRefreshTokenRotationResult> RotateForRefreshAsync(Guid appUserId, string refreshToken)
    {
        var tokens = await _uow.RefreshTokens.FindValidForRefreshAsync(appUserId, refreshToken, DateTime.UtcNow);
        var result = new BllRefreshTokenRotationResult
        {
            MatchingTokenCount = tokens.Count,
            EmptyCollectionCountText = tokens.Count == 0 ? "" : tokens.Count.ToString()
        };

        if (tokens.Count != 1)
        {
            return result;
        }

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

    // Staging only - no SaveChanges here.
    public async Task RemoveForLogoutAsync(Guid appUserId, string refreshToken)
    {
        await _uow.RefreshTokens.RemoveForLogoutAsync(appUserId, refreshToken);
    }
}
