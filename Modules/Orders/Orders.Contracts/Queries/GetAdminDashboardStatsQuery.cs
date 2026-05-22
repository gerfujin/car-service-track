using MediatR;

namespace Orders.Contracts.Queries;

public record GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsDto>;
