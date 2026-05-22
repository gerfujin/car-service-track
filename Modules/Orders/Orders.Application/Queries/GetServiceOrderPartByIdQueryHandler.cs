using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class GetServiceOrderPartByIdQueryHandler : IRequestHandler<GetServiceOrderPartByIdQuery, ServiceOrderPartDto?>
{
    private readonly IServiceOrderPartService _partService;

    public GetServiceOrderPartByIdQueryHandler(IServiceOrderPartService partService)
    {
        _partService = partService;
    }

    public async Task<ServiceOrderPartDto?> Handle(GetServiceOrderPartByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _partService.FindAsync(request.Id);
        if (entity == null) return null;

        return new ServiceOrderPartDto(
            entity.Id, entity.ServiceOrderId, entity.SparePartId, entity.SparePartName,
            entity.Quantity, entity.UnitPrice);
    }
}
