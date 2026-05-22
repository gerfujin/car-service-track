using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class GetServiceOrderByIdQueryHandler : IRequestHandler<GetServiceOrderByIdQuery, ServiceOrderDto?>
{
    private readonly IServiceOrderService _serviceOrderService;

    public GetServiceOrderByIdQueryHandler(IServiceOrderService serviceOrderService)
    {
        _serviceOrderService = serviceOrderService;
    }

    public async Task<ServiceOrderDto?> Handle(GetServiceOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var so = await _serviceOrderService.FindWithDetailsAsync(request.OrderId);
        if (so == null) return null;

        return new ServiceOrderDto(
            so.Id, so.Description, so.Status, so.OrderDate, so.CompletedDate,
            so.VehicleDisplay, so.OwnerName, so.WorkshopName, so.MechanicName,
            so.MechanicId, so.TotalAmount, so.FinalPrice, so.HasPayment);
    }
}
