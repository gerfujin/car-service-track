using Orders.Application.DTO;
using Orders.Domain;

namespace Orders.Application.Mappers;

public static class ServiceOrderMapper
{
    public static BllServiceOrder? ToBll(ServiceOrder? entity)
    {
        if (entity == null) return null;

        return new BllServiceOrder
        {
            Id = entity.Id,
            AppUserId = entity.AppUserId,
            VehicleId = entity.VehicleId,
            WorkshopId = entity.WorkshopId,
            MechanicId = entity.MechanicId,
            Description = entity.Description,
            OrderDate = entity.OrderDate,
            CompletedDate = entity.CompletedDate,
            Status = entity.Status,
            FinalPrice = entity.FinalPrice,
            ServiceIds = entity.ServiceOrderItems?
                .Select(item => item.ServiceId)
                .ToList() ?? new List<Guid>(),
            OwnerAppUserId = entity.AppUserId,
            TotalAmount =
                (entity.ServiceOrderItems != null
                    ? entity.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice)
                    : 0m) +
                (entity.ServiceOrderParts != null
                    ? entity.ServiceOrderParts.Sum(p => p.Quantity * p.UnitPrice)
                    : 0m),
            HasPayment = entity.Payment != null
        };
    }

    public static ServiceOrder? ToDomain(BllServiceOrder? bll)
    {
        if (bll == null) return null;

        return new ServiceOrder
        {
            Id = bll.Id,
            AppUserId = bll.AppUserId,
            VehicleId = bll.VehicleId,
            WorkshopId = bll.WorkshopId,
            MechanicId = bll.MechanicId,
            Description = bll.Description,
            OrderDate = bll.OrderDate,
            Status = bll.Status,
            ServiceOrderItems = bll.ServiceIds
                .Select(serviceId => new ServiceOrderItem { ServiceId = serviceId })
                .ToList()
        };
    }
}
