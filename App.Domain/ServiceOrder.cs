using System.ComponentModel.DataAnnotations;
using App.Domain.Enums;
using Base.Domain;

namespace App.Domain;

public class ServiceOrder : BaseEntity
{
    [MaxLength(512)]
    public string? Description { get; set; }

    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Pending;

    /// <summary>
    /// Admin-set final price for the order. Overrides the sum of items/parts when set.
    /// </summary>
    public decimal? FinalPrice { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // FK
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public Guid WorkshopId { get; set; }
    public Workshop? Workshop { get; set; }

    public Guid? MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }

    // Navigation
    public ICollection<ServiceOrderItem>? ServiceOrderItems { get; set; }
    public ICollection<ServiceOrderPart>? ServiceOrderParts { get; set; }
    public ICollection<ServiceOrderStatusHistory>? StatusHistory { get; set; }
    public ICollection<RepairPhoto>? RepairPhotos { get; set; }
    public Payment? Payment { get; set; }
}
