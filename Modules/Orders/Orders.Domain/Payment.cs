using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Orders.Domain.Enums;

namespace Orders.Domain;

public class Payment : BaseEntity
{
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime? PaidAt { get; set; }

    [MaxLength(64)]
    public string? PaymentMethod { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Orders-module one-to-one relationship.
    public Guid ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }
}
