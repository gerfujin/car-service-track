using App.BLL.DTO;
using App.DTO.v1.Payment;

namespace WebApp.Mappers;

/// <summary>Maps BllPayment to the API DTO (PaymentDto). No create-direction mapping needed — Create
/// is handled entirely in the BLL service (CreateForServiceOrderAsync).</summary>
public static class PaymentApiMapper
{
    public static PaymentDto ToApiDto(BllPayment p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Status = p.Status,
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
