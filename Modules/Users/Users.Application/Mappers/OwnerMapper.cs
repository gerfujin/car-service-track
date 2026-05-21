using Users.Application.DTO;
using Users.Domain;

namespace Users.Application.Mappers;

public static class OwnerMapper
{
    public static BllOwner? ToBll(Owner? entity)
    {
        if (entity == null) return null;
        return new BllOwner
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Address = entity.Address,
            Phone = entity.Phone,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            AppUserId = entity.AppUserId
        };
    }

    public static Owner? ToDomain(BllOwner? bll)
    {
        if (bll == null) return null;
        return new Owner
        {
            Id = bll.Id,
            FirstName = bll.FirstName,
            LastName = bll.LastName,
            Address = bll.Address,
            Phone = bll.Phone,
            CreatedAt = bll.CreatedAt,
            UpdatedAt = bll.UpdatedAt,
            AppUserId = bll.AppUserId
        };
    }
}
