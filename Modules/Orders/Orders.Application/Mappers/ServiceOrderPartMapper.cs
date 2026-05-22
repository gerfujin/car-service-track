using Orders.Application.DTO;
using Orders.Domain;

namespace Orders.Application.Mappers;

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
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            LineTotal = entity.Quantity * entity.UnitPrice,
            OwnerId = entity.ServiceOrder?.AppUserId
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
            UnitPrice = bll.UnitPrice
        };
    }
}
