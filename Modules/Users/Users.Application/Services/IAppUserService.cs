using Users.Application.DTO;

namespace Users.Application.Services;

public interface IAppUserService
{
    Task<IEnumerable<BllAppUser>> AllAsync();
    Task<BllAppUser?> FindAsync(Guid id);
    Task<BllAppUser?> FindWithOwnerAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
