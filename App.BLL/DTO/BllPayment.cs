using App.Domain.Enums;
using Base.Contracts;

namespace App.BLL.DTO;

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

    // Read-side projection fields (populated when navigation is loaded; null on thin write path).
    public Guid? OwnerId { get; set; }        // Vehicle.OwnerId (Owner PK)
    public string? VehicleInfo { get; set; }  // "Make Model (LicensePlate)"
    public string? WorkshopName { get; set; }
}
