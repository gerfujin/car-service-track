using App.BLL.DTO;

namespace App.BLL.Services;

public interface IRefreshTokenService
{
    Task<int?> RemoveExpiredForUserAsync(Guid appUserId);
    string AddForUser(Guid appUserId);
    Task<BllRefreshTokenRotationResult> RotateForRefreshAsync(Guid appUserId, string refreshToken);
    Task RemoveForLogoutAsync(Guid appUserId, string refreshToken);
}
