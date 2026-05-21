using App.Domain.Identity;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IRefreshTokenRepository : IBaseRepository<AppRefreshToken>
{
    Task<int?> RemoveExpiredForUserAsync(Guid appUserId, DateTime now);
    Task<List<AppRefreshToken>> FindValidForRefreshAsync(Guid appUserId, string refreshToken, DateTime now);
    Task RemoveForLogoutAsync(Guid appUserId, string refreshToken);
}
