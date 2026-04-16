using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Vehicle : BaseEntity
{
    [MaxLength(64)]
    public string Make { get; set; } = default!;

    [MaxLength(64)]
    public string Model { get; set; } = default!;

    public int Year { get; set; }

    [MaxLength(32)]
    public string LicensePlate { get; set; } = default!;

    [MaxLength(17)]
    public string? Vin { get; set; }

    public int? Mileage { get; set; }

    [MaxLength(32)]
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // FK
    public Guid OwnerId { get; set; }
    public Owner? Owner { get; set; }

    // Navigation
    public ICollection<ServiceOrder>? ServiceOrders { get; set; }
}
