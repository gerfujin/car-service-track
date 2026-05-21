using App.Domain.Enums;
using Base.Contracts;

namespace App.BLL.DTO;

public class BllServiceOrder : IBaseEntity
{
    public Guid Id { get; set; }

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

    // ---- Read-side projection fields (populated only when loaded via the detail/list
    // repository queries; null/zero on the thin write path). ----

    /// <summary>AppUser id of the order's owner (via Vehicle → Owner). Used for IDOR checks.</summary>
    public Guid? OwnerAppUserId { get; set; }

    public string? VehicleDisplay { get; set; }
    public string? WorkshopName { get; set; }
    public string? MechanicName { get; set; }

    /// <summary>Sum of ServiceOrderItems + ServiceOrderParts (qty × unit price).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Details of the selected services (names/prices) for the order detail view.</summary>
    public List<BllServiceLine>? Services { get; set; }
}
