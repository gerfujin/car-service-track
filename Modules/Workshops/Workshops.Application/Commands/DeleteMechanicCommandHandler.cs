using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;

namespace Workshops.Application.Commands;

internal sealed class DeleteMechanicCommandHandler : IRequestHandler<DeleteMechanicCommand, bool>
{
    private readonly IMechanicService _mechanicService;
    private readonly IWorkshopsUnitOfWork _uow;

    public DeleteMechanicCommandHandler(IMechanicService mechanicService, IWorkshopsUnitOfWork uow)
    {
        _mechanicService = mechanicService;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteMechanicCommand request, CancellationToken cancellationToken)
    {
        var entity = await _mechanicService.FindAsync(request.Id);
        if (entity == null) return false;

        _mechanicService.Remove(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
