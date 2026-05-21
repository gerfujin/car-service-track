namespace App.BLL.DTO;

/// <summary>
/// Neutral DTO used to transfer dashboard statistics from the BLL layer to the WebApp layer.
/// Both the Admin and the Mechanic dashboards use the same DTO; the controller maps the
/// values it needs into the area-specific ViewModel.
/// </summary>
public class BllDashboardStats
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
    public int TotalPayments { get; set; }
}
