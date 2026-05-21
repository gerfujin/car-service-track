using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class MechanicMapper
{
    public static BllMechanic? ToBll(Mechanic? entity)
    {
        if (entity == null) return null;

        return new BllMechanic
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Phone = entity.Phone,
            Email = entity.Email,
            Specialization = entity.Specialization,
        };
    }

    public static Mechanic? ToDomain(BllMechanic? bll)
    {
        if (bll == null) return null;

        return new Mechanic
        {
            Id = bll.Id,
            FirstName = bll.FirstName,
            LastName = bll.LastName,
            Phone = bll.Phone,
            Email = bll.Email,
            Specialization = bll.Specialization,
        };
    }
}
