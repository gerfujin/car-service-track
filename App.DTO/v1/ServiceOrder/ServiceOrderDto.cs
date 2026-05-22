using Orders.Domain.Enums;
using App.DTO.v1.Service;

namespace App.DTO.v1.ServiceOrder;

public class ServiceOrderDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid VehicleId { get; set; }
    public string? VehicleDisplay { get; set; }
    public Guid WorkshopId { get; set; }
    public string? WorkshopName { get; set; }
    public Guid? MechanicId { get; set; }
    public string? MechanicName { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Admin-set final price. When set, overrides the calculated TotalAmount.</summary>
    public decimal? FinalPrice { get; set; }
    public List<ServiceDto>? Services { get; set; }
}
