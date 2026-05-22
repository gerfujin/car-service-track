using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class GetAllServiceOrdersQueryHandler : IRequestHandler<GetAllServiceOrdersQuery, IEnumerable<ServiceOrderDto>>
{
    private readonly IServiceOrderService _serviceOrderService;

    public GetAllServiceOrdersQueryHandler(IServiceOrderService serviceOrderService)
    {
        _serviceOrderService = serviceOrderService;
    }

    public async Task<IEnumerable<ServiceOrderDto>> Handle(GetAllServiceOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _serviceOrderService.AllWithDetailsAsync();
        return orders.Select(so => new ServiceOrderDto(
            so.Id, so.Description, so.Status, so.OrderDate, so.CompletedDate,
            so.VehicleDisplay, so.OwnerName, so.WorkshopName, so.MechanicName,
            so.MechanicId, so.TotalAmount, so.FinalPrice, so.HasPayment));
    }
}
