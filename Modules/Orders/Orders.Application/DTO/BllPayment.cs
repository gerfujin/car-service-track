using Base.Contracts;
using Orders.Domain.Enums;

namespace Orders.Application.DTO;

public class BllPayment : IBaseEntity
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid ServiceOrderId { get; set; }

    // Read-side projection fields (populated when detail data is loaded; null on thin write path).
    public Guid? OwnerId { get; set; }
    public string? VehicleInfo { get; set; }
    public string? WorkshopName { get; set; }
}
