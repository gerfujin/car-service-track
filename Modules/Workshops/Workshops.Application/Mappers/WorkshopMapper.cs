using Base.Domain;
using Workshops.Application.DTO;
using Workshops.Domain;

namespace Workshops.Application.Mappers;

public static class WorkshopMapper
{
    public static BllWorkshop? ToBll(Workshop? entity)
    {
        if (entity == null) return null;

        return new BllWorkshop
        {
            Id = entity.Id,
            Name = entity.Name.ToString(),
            Address = entity.Address.ToString(),
            Phone = entity.Phone,
            Email = entity.Email
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
            Email = bll.Email
        };
    }
}
