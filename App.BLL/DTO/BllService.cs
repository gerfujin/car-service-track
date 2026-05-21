using Base.Contracts;

namespace App.BLL.DTO;

public class BllService : IBaseEntity
{
    public Guid Id { get; set; }

    // Multilingual Name/Description are flattened to plain strings for the current culture
    // (LangStr.ToString()); ToDomain re-wraps them in LangStr.
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }
}
