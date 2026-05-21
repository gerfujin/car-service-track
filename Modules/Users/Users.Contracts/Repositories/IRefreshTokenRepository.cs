using Base.Contracts;
using Users.Domain.Identity;

namespace Users.Contracts.Repositories;

public interface IRefreshTokenRepository : IBaseRepository<AppRefreshToken>
{
    Task<int?> RemoveExpiredForUserAsync(Guid appUserId, DateTime now);
    Task<List<AppRefreshToken>> FindValidForRefreshAsync(Guid appUserId, string refreshToken, DateTime now);
    Task RemoveForLogoutAsync(Guid appUserId, string refreshToken);
}
