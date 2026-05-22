using Orders.Domain.Enums;

namespace Orders.Contracts.Queries;

public record ServiceOrderDto(
    Guid Id,
    string? Description,
    ServiceOrderStatus Status,
    DateTime OrderDate,
    DateTime? CompletedDate,
    string? VehicleDisplay,
    string? OwnerName,
    string? WorkshopName,
    string? MechanicName,
    Guid? MechanicId,
    decimal TotalAmount,
    decimal? FinalPrice,
    bool HasPayment
);
