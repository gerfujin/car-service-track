using Base.Domain;
using Workshops.Application.DTO;
using Workshops.Domain;

namespace Workshops.Application.Mappers;

public static class ServiceMapper
{
    public static BllService? ToBll(Service? entity)
    {
        if (entity == null) return null;

        return new BllService
        {
            Id = entity.Id,
            Name = entity.Name.ToString(),
            Description = entity.Description.ToString(),
            BasePrice = entity.BasePrice,
            EstimatedTimeMinutes = entity.EstimatedTimeMinutes
        };
    }

    public static Service? ToDomain(BllService? bll)
    {
        if (bll == null) return null;

        return new Service
        {
            Id = bll.Id,
            Name = new LangStr(bll.Name),
            Description = new LangStr(bll.Description ?? string.Empty),
            BasePrice = bll.BasePrice,
            EstimatedTimeMinutes = bll.EstimatedTimeMinutes
        };
    }
}
