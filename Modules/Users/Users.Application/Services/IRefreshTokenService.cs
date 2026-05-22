using Users.Application.DTO;

namespace Users.Application.Services;

public interface IRefreshTokenService
{
    Task<int?> RemoveExpiredForUserAsync(Guid appUserId);
    Task<string> AddForUserAsync(Guid appUserId);
    Task<BllRefreshTokenRotationResult> RotateForRefreshAsync(Guid appUserId, string refreshToken);
    Task RemoveForLogoutAsync(Guid appUserId, string refreshToken);
    Task<int> SaveChangesAsync();
}
