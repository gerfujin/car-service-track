using Base.Contracts;
using Users.Contracts.Repositories;

namespace Users.Contracts;

public interface IUsersUnitOfWork : IBaseUnitOfWork
{
    IAppUserRepository AppUsers { get; }
    IOwnerRepository Owners { get; }
    IVehicleRepository Vehicles { get; }
    IRefreshTokenRepository RefreshTokens { get; }
}
