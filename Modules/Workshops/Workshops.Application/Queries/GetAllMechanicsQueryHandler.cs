using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetAllMechanicsQueryHandler : IRequestHandler<GetAllMechanicsQuery, IEnumerable<MechanicDto>>
{
    private readonly IMechanicService _mechanicService;

    public GetAllMechanicsQueryHandler(IMechanicService mechanicService)
    {
        _mechanicService = mechanicService;
    }

    public async Task<IEnumerable<MechanicDto>> Handle(GetAllMechanicsQuery request, CancellationToken cancellationToken)
    {
        var mechanics = await _mechanicService.AllAsync();
        return mechanics.Select(m => new MechanicDto(
            m.Id, m.FirstName, m.LastName, m.Phone, m.Email, m.Specialization, m.AppUserId));
    }
}
