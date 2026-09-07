using Users.Application.DTO;
using Users.Contracts;

namespace Users.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IAppUserService _appUsers;
    private readonly IOwnerService _owners;
    private readonly IUsersUnitOfWork _uow;

    public ProfileService(IAppUserService appUsers, IOwnerService owners, IUsersUnitOfWork uow)
    {
        _appUsers = appUsers;
        _owners = owners;
        _uow = uow;
    }

    public async Task<BllProfile?> GetProfileAsync(Guid appUserId)
    {
        var appUser = await _appUsers.FindAsync(appUserId);
        if (appUser == null) return null;

        // Lazy-creation pattern (matches VehiclesController.CreateVehicle) — a profile view
        // must never 404 just because no Owner row exists yet (e.g. admin/mechanic accounts).
        var owner = await _owners.EnsureForUserAsync(appUserId, appUser.UserName ?? appUser.Email ?? "User");
        await _uow.SaveChangesAsync();

        return ToProfile(appUser, owner);
    }

    public async Task<BllProfile?> UpdateProfileAsync(Guid appUserId, BllProfileUpdate update)
    {
        var appUser = await _appUsers.FindAsync(appUserId);
        if (appUser == null) return null;

        // Load-then-merge onto the existing Owner (preserves CreatedAt/Id), or create one if
        // this is the user's first edit — EnsureForUserAsync's staged-but-unsaved entity can't
        // be found again via Update() before a save, so the create/merge branches are explicit.
        var existingOwner = await _owners.FindByUserAsync(appUserId);
        BllOwner owner;
        if (existingOwner == null)
        {
            owner = _owners.Add(new BllOwner
            {
                AppUserId = appUserId,
                FirstName = update.FirstName,
                LastName = update.LastName,
                Address = update.Address,
                Phone = update.Phone,
            });
        }
        else
        {
            existingOwner.FirstName = update.FirstName;
            existingOwner.LastName = update.LastName;
            existingOwner.Address = update.Address;
            existingOwner.Phone = update.Phone;
            owner = _owners.Update(existingOwner);
        }

        await _uow.SaveChangesAsync();

        return ToProfile(appUser, owner);
    }

    private static BllProfile ToProfile(BllAppUser appUser, BllOwner owner) => new()
    {
        AppUserId = appUser.Id,
        Email = appUser.Email ?? string.Empty,
        FirstName = owner.FirstName,
        LastName = owner.LastName,
        Address = owner.Address,
        Phone = owner.Phone,
    };
}
