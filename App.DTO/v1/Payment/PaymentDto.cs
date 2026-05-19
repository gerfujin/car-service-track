using App.Domain.Enums;

namespace App.DTO.v1.Payment;

public class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public Guid ServiceOrderId { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Nested convenience fields
    public string? VehicleInfo { get; set; }   // "Make Model (LicensePlate)"
    public string? WorkshopName { get; set; }
}
