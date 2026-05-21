using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetMechanicByIdQueryHandler : IRequestHandler<GetMechanicByIdQuery, MechanicDto?>
{
    private readonly IMechanicService _mechanicService;

    public GetMechanicByIdQueryHandler(IMechanicService mechanicService)
    {
        _mechanicService = mechanicService;
    }

    public async Task<MechanicDto?> Handle(GetMechanicByIdQuery request, CancellationToken cancellationToken)
    {
        var mechanic = await _mechanicService.FindAsync(request.MechanicId);
        if (mechanic == null)
            return null;

        return new MechanicDto(
            mechanic.Id,
            mechanic.FirstName,
            mechanic.LastName,
            mechanic.Phone,
            mechanic.Email,
            mechanic.Specialization,
            mechanic.AppUserId
        );
    }
}
