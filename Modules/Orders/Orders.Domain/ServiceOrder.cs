using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Orders.Domain.Enums;

namespace Orders.Domain;

public class ServiceOrder : BaseEntity
{
    [MaxLength(512)]
    public string? Description { get; set; }

    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Pending;

    public decimal? FinalPrice { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Denormalized owner identity for IDOR checks.
    public Guid AppUserId { get; set; }

    // Cross-module references stored as plain ids.
    public Guid VehicleId { get; set; }
    public Guid WorkshopId { get; set; }
    public Guid? MechanicId { get; set; }

    // Orders-module navigation stays.
    public ICollection<ServiceOrderItem>? ServiceOrderItems { get; set; }
    public ICollection<ServiceOrderPart>? ServiceOrderParts { get; set; }
    public ICollection<ServiceOrderStatusHistory>? StatusHistory { get; set; }
    public ICollection<RepairPhoto>? RepairPhotos { get; set; }
    public Payment? Payment { get; set; }
}
