using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace Workshops.Domain;

public class Mechanic : BaseEntity
{
    [MaxLength(128)]
    public string FirstName { get; set; } = default!;

    [MaxLength(128)]
    public string LastName { get; set; } = default!;

    [MaxLength(256)]
    public string? Specialization { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid AppUserId { get; set; }

    public ICollection<MechanicInWorkshop>? MechanicsInWorkshop { get; set; }
}
