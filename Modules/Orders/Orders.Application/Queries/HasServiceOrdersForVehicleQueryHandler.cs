using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class HasServiceOrdersForVehicleQueryHandler : IRequestHandler<HasServiceOrdersForVehicleQuery, bool>
{
    private readonly IServiceOrderService _serviceOrders;

    public HasServiceOrdersForVehicleQueryHandler(IServiceOrderService serviceOrders)
    {
        _serviceOrders = serviceOrders;
    }

    public async Task<bool> Handle(HasServiceOrdersForVehicleQuery request, CancellationToken cancellationToken)
    {
        return await _serviceOrders.HasOrdersForVehicleAsync(request.VehicleId);
    }
}
