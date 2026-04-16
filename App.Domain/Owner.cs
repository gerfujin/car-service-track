using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using Base.Domain;

namespace App.Domain;

public class Owner : BaseEntity
{
    [MaxLength(128)]
    public string FirstName { get; set; } = default!;

    [MaxLength(128)]
    public string LastName { get; set; } = default!;

    [MaxLength(256)]
    public string? Address { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // FK to Identity user
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    // Navigation
    public ICollection<Vehicle>? Vehicles { get; set; }
}
