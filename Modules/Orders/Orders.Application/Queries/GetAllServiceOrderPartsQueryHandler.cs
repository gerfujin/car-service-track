using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class GetAllServiceOrderPartsQueryHandler : IRequestHandler<GetAllServiceOrderPartsQuery, IEnumerable<ServiceOrderPartDto>>
{
    private readonly IServiceOrderPartService _partService;

    public GetAllServiceOrderPartsQueryHandler(IServiceOrderPartService partService)
    {
        _partService = partService;
    }

    public async Task<IEnumerable<ServiceOrderPartDto>> Handle(GetAllServiceOrderPartsQuery request, CancellationToken cancellationToken)
    {
        var all = await _partService.AllAsync();
        var filtered = request.ServiceOrderId.HasValue
            ? all.Where(x => x.ServiceOrderId == request.ServiceOrderId.Value)
            : all;

        return filtered.Select(x => new ServiceOrderPartDto(
            x.Id, x.ServiceOrderId, x.SparePartId, x.SparePartName, x.Quantity, x.UnitPrice));
    }
}
