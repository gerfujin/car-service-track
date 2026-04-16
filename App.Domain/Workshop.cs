using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Workshop : BaseEntity
{
    // LangStr for translatable name
    public LangStr Name { get; set; } = new LangStr();

    // LangStr for translatable address
    public LangStr Address { get; set; } = new LangStr();

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MechanicInWorkshop>? MechanicsInWorkshop { get; set; }
    public ICollection<ServiceOrder>? ServiceOrders { get; set; }
}
