using App.DTO.v1.Profile;
using Users.Application.DTO;

namespace Users.Presentation.Mappers;

/// <summary>
/// Maps between the module BLL DTO (Users.Application.DTO.BllProfile) and the API DTOs (App.DTO.v1.Profile.*).
/// </summary>
public static class ProfileApiMapper
{
    public static ProfileDto ToApiDto(BllProfile bll) => new()
    {
        AppUserId = bll.AppUserId,
        Email = bll.Email,
        FirstName = bll.FirstName,
        LastName = bll.LastName,
        Address = bll.Address,
        Phone = bll.Phone,
    };

    public static BllProfileUpdate ToBll(ProfileUpdateDto dto) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Address = dto.Address,
        Phone = dto.Phone,
    };
}
