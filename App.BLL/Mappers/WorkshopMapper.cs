using App.BLL.DTO;
using App.Domain;
using Base.Domain;

namespace App.BLL.Mappers;

public static class WorkshopMapper
{
    public static BllWorkshop? ToBll(Workshop? entity)
    {
        if (entity == null) return null;

        return new BllWorkshop
        {
            Id = entity.Id,
            // Both Name and Address are LangStr -> current-culture string.
            Name = entity.Name.ToString(),
            Address = entity.Address.ToString(),
            Phone = entity.Phone,
            Email = entity.Email,
        };
    }

    public static Workshop? ToDomain(BllWorkshop? bll)
    {
        if (bll == null) return null;

        return new Workshop
        {
            Id = bll.Id,
            Name = new LangStr(bll.Name),
            Address = new LangStr(bll.Address),
            Phone = bll.Phone,
            Email = bll.Email,
        };
    }
}
