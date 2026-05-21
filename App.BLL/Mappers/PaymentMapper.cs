using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class PaymentMapper
{
    public static BllPayment? ToBll(Payment? entity)
    {
        if (entity == null) return null;
        return new BllPayment
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Status = entity.Status,
            PaidAt = entity.PaidAt,
            PaymentMethod = entity.PaymentMethod,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ServiceOrderId = entity.ServiceOrderId,
            // Navigation projection (null when not loaded)
            OwnerId = entity.ServiceOrder?.Vehicle?.OwnerId,
            VehicleInfo = entity.ServiceOrder?.Vehicle != null
                ? $"{entity.ServiceOrder.Vehicle.Make} {entity.ServiceOrder.Vehicle.Model} ({entity.ServiceOrder.Vehicle.LicensePlate})"
                : null,
            WorkshopName = entity.ServiceOrder?.Workshop?.Name.ToString()
        };
    }

    public static Payment? ToDomain(BllPayment? bll)
    {
        if (bll == null) return null;
        return new Payment
        {
            Id = bll.Id,
            Amount = bll.Amount,
            Status = bll.Status,
            PaidAt = bll.PaidAt,
            PaymentMethod = bll.PaymentMethod,
            Notes = bll.Notes,
            CreatedAt = bll.CreatedAt,
            UpdatedAt = bll.UpdatedAt,
            ServiceOrderId = bll.ServiceOrderId
        };
    }
}
