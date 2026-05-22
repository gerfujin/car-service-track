using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetAllWorkshopsQueryHandler : IRequestHandler<GetAllWorkshopsQuery, IEnumerable<WorkshopDto>>
{
    private readonly IWorkshopService _workshopService;

    public GetAllWorkshopsQueryHandler(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    public async Task<IEnumerable<WorkshopDto>> Handle(GetAllWorkshopsQuery request, CancellationToken cancellationToken)
    {
        var workshops = await _workshopService.AllAsync();
        return workshops.Select(w => new WorkshopDto(w.Id, w.Name, w.Address, w.Phone, w.Email));
    }
}
