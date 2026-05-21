using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class ServiceOrderMapper
{
    public static BllServiceOrder? ToBll(ServiceOrder? entity)
    {
        if (entity == null) return null;

        return new BllServiceOrder
        {
            Id = entity.Id,
            VehicleId = entity.VehicleId,
            WorkshopId = entity.WorkshopId,
            MechanicId = entity.MechanicId,
            Description = entity.Description,
            OrderDate = entity.OrderDate,
            CompletedDate = entity.CompletedDate,
            Status = entity.Status,
            FinalPrice = entity.FinalPrice,
            // Flatten the items relation down to the selected service ids.
            ServiceIds = entity.ServiceOrderItems?
                .Select(item => item.ServiceId)
                .ToList() ?? new List<Guid>(),

            // Read-side projection (null/0 when the related data isn't loaded).
            OwnerAppUserId = entity.Vehicle?.Owner?.AppUserId,
            VehicleDisplay = entity.Vehicle != null
                ? $"{entity.Vehicle.Make} {entity.Vehicle.Model} ({entity.Vehicle.LicensePlate})"
                : null,
            WorkshopName = entity.Workshop != null ? entity.Workshop.Name.ToString() : null,
            MechanicName = entity.Mechanic != null
                ? $"{entity.Mechanic.FirstName} {entity.Mechanic.LastName}"
                : null,
            OwnerName = entity.Vehicle?.Owner != null
                ? $"{entity.Vehicle.Owner.FirstName} {entity.Vehicle.Owner.LastName}"
                : null,
            TotalAmount =
                (entity.ServiceOrderItems != null
                    ? entity.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice)
                    : 0m) +
                (entity.ServiceOrderParts != null
                    ? entity.ServiceOrderParts.Sum(p => p.Quantity * p.UnitPrice)
                    : 0m),
            Services = entity.ServiceOrderItems?
                .Where(item => item.Service != null)
                .Select(item => new BllServiceLine
                {
                    Id = item.Service!.Id,
                    Name = item.Service!.Name.ToString() ?? string.Empty,
                    Description = item.Service!.Description.ToString(),
                    BasePrice = item.Service!.BasePrice,
                    EstimatedTimeMinutes = item.Service!.EstimatedTimeMinutes
                })
                .ToList(),
            HasPayment = entity.Payment != null,
        };
    }

    public static ServiceOrder? ToDomain(BllServiceOrder? bll)
    {
        if (bll == null) return null;

        return new ServiceOrder
        {
            Id = bll.Id,
            VehicleId = bll.VehicleId,
            WorkshopId = bll.WorkshopId,
            MechanicId = bll.MechanicId,
            Description = bll.Description,
            OrderDate = bll.OrderDate,
            Status = bll.Status,
            // Re-create the item links from the flattened service ids.
            ServiceOrderItems = bll.ServiceIds
                .Select(serviceId => new ServiceOrderItem { ServiceId = serviceId })
                .ToList(),
        };
    }
}
