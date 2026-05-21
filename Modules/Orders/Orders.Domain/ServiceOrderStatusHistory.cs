using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Orders.Domain.Enums;

namespace Orders.Domain;

public class ServiceOrderStatusHistory : BaseEntity
{
    public ServiceOrderStatus Status { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Orders-module relationship.
    public Guid ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }
}
