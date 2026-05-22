using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class UpdateMechanicCommandHandler : IRequestHandler<UpdateMechanicCommand, MechanicDto?>
{
    private readonly IMechanicService _mechanicService;
    private readonly IWorkshopsUnitOfWork _uow;

    public UpdateMechanicCommandHandler(IMechanicService mechanicService, IWorkshopsUnitOfWork uow)
    {
        _mechanicService = mechanicService;
        _uow = uow;
    }

    public async Task<MechanicDto?> Handle(UpdateMechanicCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllMechanic
        {
            Id = request.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            Specialization = request.Specialization
        };

        var updated = await _mechanicService.UpdateAsync(bll);
        if (updated == null) return null;

        await _uow.SaveChangesAsync();

        return new MechanicDto(updated.Id, updated.FirstName, updated.LastName, updated.Phone, updated.Email, updated.Specialization, updated.AppUserId);
    }
}
