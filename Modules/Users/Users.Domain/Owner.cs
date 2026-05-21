using System.ComponentModel.DataAnnotations;
using Base.Domain;
using Users.Domain.Identity;

namespace Users.Domain;

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

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public ICollection<Vehicle>? Vehicles { get; set; }
}
