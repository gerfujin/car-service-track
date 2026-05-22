using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetAllServicesQueryHandler : IRequestHandler<GetAllServicesQuery, IEnumerable<ServiceDto>>
{
    private readonly IServiceService _serviceService;

    public GetAllServicesQueryHandler(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    public async Task<IEnumerable<ServiceDto>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await _serviceService.AllAsync();
        return services.Select(s => new ServiceDto(
            s.Id, s.Name, s.Description, s.BasePrice, s.EstimatedTimeMinutes));
    }
}
