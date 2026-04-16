using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class SparePart : BaseEntity
{
    // LangStr for translatable name
    public LangStr Name { get; set; } = new LangStr();

    [MaxLength(64)]
    public string? PartNumber { get; set; }

    public decimal UnitPrice { get; set; }

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ServiceOrderPart>? ServiceOrderParts { get; set; }
}
