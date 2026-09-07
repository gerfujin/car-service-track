using Orders.Domain.Enums;

namespace Orders.Presentation.Areas.Mechanic.ViewModels;

public class MechanicOrderViewModel
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? VehicleDisplay { get; set; }
    public string? WorkshopName { get; set; }
    public string? MechanicName { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasPayment { get; set; }
}

public class MechanicOrderListViewModel
{
    public List<MechanicOrderViewModel> Orders { get; set; } = new();
}

public class MechanicUpdateStatusViewModel
{
    public Guid Id { get; set; }
    public ServiceOrderStatus CurrentStatus { get; set; }
    public ServiceOrderStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string? VehicleDisplay { get; set; }
    public string? WorkshopName { get; set; }
    public string? Description { get; set; }
}
