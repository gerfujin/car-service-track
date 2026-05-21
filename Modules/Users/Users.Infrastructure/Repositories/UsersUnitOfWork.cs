using Users.Contracts;
using Users.Contracts.Repositories;

namespace Users.Infrastructure.Repositories;

public class UsersUnitOfWork : IUsersUnitOfWork
{
    private readonly UsersDbContext _context;

    private IAppUserRepository? _appUsers;
    private IOwnerRepository? _owners;
    private IVehicleRepository? _vehicles;
    private IRefreshTokenRepository? _refreshTokens;

    public UsersUnitOfWork(UsersDbContext context)
    {
        _context = context;
    }

    public IAppUserRepository AppUsers =>
        _appUsers ??= new AppUserRepository(_context);

    public IOwnerRepository Owners =>
        _owners ??= new OwnerRepository(_context);

    public IVehicleRepository Vehicles =>
        _vehicles ??= new VehicleRepository(_context);

    public IRefreshTokenRepository RefreshTokens =>
        _refreshTokens ??= new RefreshTokenRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
