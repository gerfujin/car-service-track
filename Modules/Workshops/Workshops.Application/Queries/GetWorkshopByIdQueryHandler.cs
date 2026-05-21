using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetWorkshopByIdQueryHandler : IRequestHandler<GetWorkshopByIdQuery, WorkshopDto?>
{
    private readonly IWorkshopService _workshopService;

    public GetWorkshopByIdQueryHandler(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    public async Task<WorkshopDto?> Handle(GetWorkshopByIdQuery request, CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.FindAsync(request.WorkshopId);
        if (workshop == null)
            return null;

        return new WorkshopDto(
            workshop.Id,
            workshop.Name,
            workshop.Address,
            workshop.Phone,
            workshop.Email
        );
    }
}
