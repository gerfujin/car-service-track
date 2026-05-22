namespace Orders.Contracts.Queries;

public record AdminDashboardStatsDto(
    int TotalVehicles,
    int TotalServiceOrders,
    int TotalWorkshops,
    int TotalMechanics,
    int TotalClients,
    int PendingOrders,
    int InProgressOrders,
    int CompletedOrders,
    decimal TotalRevenue
);
