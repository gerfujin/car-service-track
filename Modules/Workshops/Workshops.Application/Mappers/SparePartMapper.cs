using Base.Domain;
using Workshops.Application.DTO;
using Workshops.Domain;

namespace Workshops.Application.Mappers;

public static class SparePartMapper
{
    public static BllSparePart? ToBll(SparePart? entity)
    {
        if (entity == null) return null;

        return new BllSparePart
        {
            Id = entity.Id,
            Name = entity.Name.Translate() ?? entity.Name.ToString() ?? "",
            PartNumber = entity.PartNumber,
            Country = entity.Country,
            UnitPrice = entity.UnitPrice,
            StockQuantity = entity.StockQuantity
        };
    }

    public static SparePart? ToDomain(BllSparePart? bll)
    {
        if (bll == null) return null;

        return new SparePart
        {
            Id = bll.Id,
            Name = new LangStr(bll.Name),
            PartNumber = bll.PartNumber,
            Country = bll.Country,
            UnitPrice = bll.UnitPrice,
            StockQuantity = bll.StockQuantity
        };
    }
}
