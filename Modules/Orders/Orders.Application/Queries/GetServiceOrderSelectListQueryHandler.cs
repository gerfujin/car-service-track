using MediatR;
using Orders.Application.Services;
using Orders.Contracts.Queries;

namespace Orders.Application.Queries;

internal sealed class GetServiceOrderSelectListQueryHandler : IRequestHandler<GetServiceOrderSelectListQuery, IEnumerable<SelectListItemDto>>
{
    private readonly IServiceOrderService _serviceOrderService;

    public GetServiceOrderSelectListQueryHandler(IServiceOrderService serviceOrderService)
    {
        _serviceOrderService = serviceOrderService;
    }

    public async Task<IEnumerable<SelectListItemDto>> Handle(GetServiceOrderSelectListQuery request, CancellationToken cancellationToken)
    {
        var items = await _serviceOrderService.GetSelectListForAdminAsync();
        return items.Select(i => new SelectListItemDto(i.Value, i.Text));
    }
}
