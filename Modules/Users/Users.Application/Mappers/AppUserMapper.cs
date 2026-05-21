using Users.Application.DTO;
using Users.Domain.Identity;

namespace Users.Application.Mappers;

public static class AppUserMapper
{
    public static BllAppUser? ToBll(AppUser? entity)
    {
        if (entity == null) return null;

        return new BllAppUser
        {
            Id = entity.Id,
            Email = entity.Email,
            UserName = entity.UserName,
            FirstName = entity.Owner?.FirstName,
            LastName = entity.Owner?.LastName
        };
    }

    public static AppUser? ToDomain(BllAppUser? bll)
    {
        if (bll == null) return null;

        return new AppUser
        {
            Id = bll.Id,
            Email = bll.Email,
            UserName = bll.UserName
        };
    }
}
