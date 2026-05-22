using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class UpdateWorkshopCommandHandler : IRequestHandler<UpdateWorkshopCommand, WorkshopDto?>
{
    private readonly IWorkshopService _workshopService;
    private readonly IWorkshopsUnitOfWork _uow;

    public UpdateWorkshopCommandHandler(IWorkshopService workshopService, IWorkshopsUnitOfWork uow)
    {
        _workshopService = workshopService;
        _uow = uow;
    }

    public async Task<WorkshopDto?> Handle(UpdateWorkshopCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllWorkshop
        {
            Id = request.Id,
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email
        };

        var updated = await _workshopService.UpdateAsync(bll);
        if (updated == null) return null;

        await _uow.SaveChangesAsync();

        return new WorkshopDto(updated.Id, updated.Name, updated.Address, updated.Phone, updated.Email);
    }
}
