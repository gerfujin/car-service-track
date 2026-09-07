namespace Orders.Presentation.Areas.Mechanic.ViewModels;

public class MechanicDashboardViewModel
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int InProgressOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int TotalVehicles { get; set; }
    public int TotalPayments { get; set; }
}
