using Users.Application.DTO;
using Users.Application.Mappers;
using Users.Contracts;

namespace Users.Application.Services;

public class AppUserService : IAppUserService
{
    private readonly IUsersUnitOfWork _uow;

    public AppUserService(IUsersUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllAppUser>> AllAsync()
    {
        var entities = await _uow.AppUsers.AllAsync();
        return entities.Select(e => AppUserMapper.ToBll(e)!).ToList();
    }

    public async Task<BllAppUser?> FindAsync(Guid id)
    {
        return AppUserMapper.ToBll(await _uow.AppUsers.FindAsync(id));
    }

    public async Task<BllAppUser?> FindWithOwnerAsync(Guid id)
    {
        return AppUserMapper.ToBll(await _uow.AppUsers.FindWithOwnerAsync(id));
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.AppUsers.ExistsAsync(id);
    }
}
