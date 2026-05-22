using App.Domain.Enums;
using App.DTO.v1.Payment;
using Orders.Application.DTO;

namespace WebApp.Mappers;

/// <summary>
/// Maps Orders.Application.DTO.BllPayment to the API DTO (PaymentDto).
/// No create-direction mapping needed — Create is handled entirely in the BLL service (CreateForServiceOrderAsync).
/// Note: Orders.Domain.Enums.PaymentStatus and App.Domain.Enums.PaymentStatus share identical numeric
/// values (Pending=0, Paid=1, PartiallyPaid=2, Refunded=3, Cancelled=4), so the int-cast is lossless.
/// </summary>
public static class PaymentApiMapper
{
    public static PaymentDto ToApiDto(BllPayment p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Status = (PaymentStatus)(int)p.Status,
        PaidAt = p.PaidAt,
        PaymentMethod = p.PaymentMethod,
        Notes = p.Notes,
        ServiceOrderId = p.ServiceOrderId,
        OwnerId = p.OwnerId,
        CreatedAt = p.CreatedAt,
        VehicleInfo = p.VehicleInfo,
        WorkshopName = p.WorkshopName
    };
}
