using Base.Contracts;

namespace App.BLL.DTO;

public class BllServiceOrderPart : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public Guid SparePartId { get; set; }
    public string? SparePartName { get; set; }
    public string? SparePartPartNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Read-side ownership projection used for API IDOR checks. Not exposed by API DTOs.
    public Guid? OwnerId { get; set; }
}
