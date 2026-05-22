using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class CreateMechanicCommandHandler : IRequestHandler<CreateMechanicCommand, MechanicDto>
{
    private readonly IMechanicService _mechanicService;
    private readonly IWorkshopsUnitOfWork _uow;

    public CreateMechanicCommandHandler(IMechanicService mechanicService, IWorkshopsUnitOfWork uow)
    {
        _mechanicService = mechanicService;
        _uow = uow;
    }

    public async Task<MechanicDto> Handle(CreateMechanicCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllMechanic
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            Specialization = request.Specialization
        };

        var added = _mechanicService.Add(bll);
        await _uow.SaveChangesAsync();

        return new MechanicDto(added.Id, added.FirstName, added.LastName, added.Phone, added.Email, added.Specialization, added.AppUserId);
    }
}
