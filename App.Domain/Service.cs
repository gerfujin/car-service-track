using Base.Domain;

namespace App.Domain;

public class Service : BaseEntity
{
    // LangStr for translatable name
    public LangStr Name { get; set; } = new LangStr();

    // LangStr for translatable description
    public LangStr Description { get; set; } = new LangStr();

    public decimal BasePrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ServiceOrderItem>? ServiceOrderItems { get; set; }
}
