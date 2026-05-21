using Base.Domain;

namespace Orders.Domain;

public class ServiceOrderPart : BaseEntity
{
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    // Orders-module relationship.
    public Guid ServiceOrderId { get; set; }
    public ServiceOrder? ServiceOrder { get; set; }

    // Cross-module reference stored as plain id.
    public Guid SparePartId { get; set; }
}
