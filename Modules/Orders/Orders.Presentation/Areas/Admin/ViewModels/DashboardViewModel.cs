namespace WebApp.Areas.Admin.ViewModels;

public class DashboardViewModel : AdminPageViewModelBase
{
    public int TotalVehicles { get; set; }
    public int TotalServiceOrders { get; set; }
    public int TotalWorkshops { get; set; }
    public int TotalMechanics { get; set; }
    public int TotalClients { get; set; }
    public int PendingOrders { get; set; }
    public int InProgressOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}
