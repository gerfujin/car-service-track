using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace Orders.Domain;

public class RepairPhoto : BaseEntity
{
    [MaxLength(512)]
    public string? FilePath { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Orders-module relationship.
    public Guid ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }
}
