using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    private readonly IServiceOrderService _serviceOrderService;

    public GetAdminDashboardStatsQueryHandler(IServiceOrderService serviceOrderService)
    {
        _serviceOrderService = serviceOrderService;
    }

    public async Task<AdminDashboardStatsDto> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _serviceOrderService.GetAdminDashboardStatsAsync();
        return new AdminDashboardStatsDto(
            stats.TotalVehicles,
            stats.TotalServiceOrders,
            stats.TotalWorkshops,
            stats.TotalMechanics,
            stats.TotalClients,
            stats.PendingOrders,
            stats.InProgressOrders,
            stats.CompletedOrders,
            stats.TotalRevenue
        );
    }
}
