using Base.Domain;

namespace App.Domain;

public class ServiceOrderPart : BaseEntity
{
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    // FK
    public Guid ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }

    public Guid SparePartId { get; set; }
    public SparePart? SparePart { get; set; }
}
