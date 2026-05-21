using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace Workshops.Domain;

public class Workshop : BaseEntity
{
    public LangStr Name { get; set; } = new LangStr();

    public LangStr Address { get; set; } = new LangStr();

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MechanicInWorkshop>? MechanicsInWorkshop { get; set; }
}
