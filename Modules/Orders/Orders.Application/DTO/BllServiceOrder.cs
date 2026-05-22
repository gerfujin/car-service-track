using Base.Contracts;
using Orders.Domain.Enums;

namespace Orders.Application.DTO;

public class BllServiceOrder : IBaseEntity
{
    public Guid Id { get; set; }

    public Guid AppUserId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid WorkshopId { get; set; }
    public Guid? MechanicId { get; set; }

    public string? Description { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public decimal? FinalPrice { get; set; }

    // Flattened form of the ServiceOrderItems relation: the selected service ids
    // (drives the multi-select services feature; used by the write/update path).
    public List<Guid> ServiceIds { get; set; } = new();

    // Read-side projection fields (populated when loaded via detail/list queries).
    public Guid? OwnerAppUserId { get; set; }
    public string? VehicleDisplay { get; set; }
    public string? WorkshopName { get; set; }
    public string? MechanicName { get; set; }
    public string? OwnerName { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasPayment { get; set; }
    public List<BllServiceLine>? Services { get; set; }
}
