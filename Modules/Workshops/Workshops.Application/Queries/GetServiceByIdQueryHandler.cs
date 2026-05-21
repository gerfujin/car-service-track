using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ServiceDto?>
{
    private readonly IServiceService _serviceService;

    public GetServiceByIdQueryHandler(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    public async Task<ServiceDto?> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _serviceService.FindAsync(request.ServiceId);
        if (service == null)
            return null;

        return new ServiceDto(
            service.Id,
            service.Name,
            service.Description,
            service.BasePrice,
            service.EstimatedTimeMinutes
        );
    }
}
