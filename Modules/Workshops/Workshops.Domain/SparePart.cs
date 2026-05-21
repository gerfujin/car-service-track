using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace Workshops.Domain;

public class SparePart : BaseEntity
{
    public LangStr Name { get; set; } = new LangStr();

    [MaxLength(64)]
    public string? PartNumber { get; set; }

    [MaxLength(64)]
    public string? Country { get; set; }

    public decimal UnitPrice { get; set; }

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
