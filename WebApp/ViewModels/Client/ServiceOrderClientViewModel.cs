using Orders.Domain.Enums;

namespace WebApp.ViewModels.Client;

public class ServiceOrderClientViewModel
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

public class ServiceOrderClientListViewModel
{
    public List<ServiceOrderClientViewModel> Orders { get; set; } = new();
    public bool CanUpdateStatus { get; set; }
}
