using Base.Contracts;

namespace App.BLL.DTO;

public class BllSparePart : IBaseEntity
{
    public Guid Id { get; set; }

    // Name is multilingual on the entity (LangStr); flattened to current-culture string here.
    public string Name { get; set; } = default!;
    public string? PartNumber { get; set; }
    public string? Country { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
}
