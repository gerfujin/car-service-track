using Orders.Application.DTO;
using Orders.Domain;

namespace Orders.Application.Mappers;

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
            OwnerId = entity.ServiceOrder?.AppUserId,
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
