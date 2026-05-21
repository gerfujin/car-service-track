using Base.Contracts;

namespace Workshops.Application.DTO;

public class BllSparePart : IBaseEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? PartNumber { get; set; }
    public string? Country { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
}
