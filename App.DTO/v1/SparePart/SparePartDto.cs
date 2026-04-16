namespace App.DTO.v1.SparePart;

public class SparePartDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? PartNumber { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
}
