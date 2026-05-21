using Base.Domain;

namespace Workshops.Domain;

public class Service : BaseEntity
{
    public LangStr Name { get; set; } = new LangStr();

    public LangStr Description { get; set; } = new LangStr();

    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
