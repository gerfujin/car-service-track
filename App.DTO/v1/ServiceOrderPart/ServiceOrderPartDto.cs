namespace App.DTO.v1.ServiceOrderPart;

public class ServiceOrderPartDto
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public Guid SparePartId { get; set; }
    public string? SparePartName { get; set; }
    public string? SparePartPartNumber { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
}
