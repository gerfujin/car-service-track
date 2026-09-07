using Users.Application.DTO;

namespace Users.Application.Services;

public interface IProfileService
{
    Task<BllProfile?> GetProfileAsync(Guid appUserId);

    Task<BllProfile?> UpdateProfileAsync(Guid appUserId, BllProfileUpdate update);
}
