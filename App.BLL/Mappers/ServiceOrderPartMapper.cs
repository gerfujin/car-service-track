using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class ServiceOrderPartMapper
{
    public static BllServiceOrderPart? ToBll(ServiceOrderPart? entity)
    {
        if (entity == null) return null;

        return new BllServiceOrderPart
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            SparePartName = entity.SparePart != null
                ? entity.SparePart.Name.Translate() ?? entity.SparePart.Name.ToString()
                : null,
            SparePartPartNumber = entity.SparePart?.PartNumber,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            LineTotal = entity.Quantity * entity.UnitPrice,
            OwnerId = entity.ServiceOrder?.Vehicle?.OwnerId,
        };
    }

    public static ServiceOrderPart? ToDomain(BllServiceOrderPart? bll)
    {
        if (bll == null) return null;

        return new ServiceOrderPart
        {
            Id = bll.Id,
            ServiceOrderId = bll.ServiceOrderId,
            SparePartId = bll.SparePartId,
            Quantity = bll.Quantity,
            UnitPrice = bll.UnitPrice,
        };
    }
}
