using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class CreateWorkshopCommandHandler : IRequestHandler<CreateWorkshopCommand, WorkshopDto>
{
    private readonly IWorkshopService _workshopService;
    private readonly IWorkshopsUnitOfWork _uow;

    public CreateWorkshopCommandHandler(IWorkshopService workshopService, IWorkshopsUnitOfWork uow)
    {
        _workshopService = workshopService;
        _uow = uow;
    }

    public async Task<WorkshopDto> Handle(CreateWorkshopCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllWorkshop
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email
        };

        var added = _workshopService.Add(bll);
        await _uow.SaveChangesAsync();

        return new WorkshopDto(added.Id, added.Name, added.Address, added.Phone, added.Email);
    }
}
