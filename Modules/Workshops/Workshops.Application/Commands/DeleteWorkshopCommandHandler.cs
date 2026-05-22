using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;

namespace Workshops.Application.Commands;

internal sealed class DeleteWorkshopCommandHandler : IRequestHandler<DeleteWorkshopCommand, bool>
{
    private readonly IWorkshopService _workshopService;
    private readonly IWorkshopsUnitOfWork _uow;

    public DeleteWorkshopCommandHandler(IWorkshopService workshopService, IWorkshopsUnitOfWork uow)
    {
        _workshopService = workshopService;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteWorkshopCommand request, CancellationToken cancellationToken)
    {
        var entity = await _workshopService.FindAsync(request.Id);
        if (entity == null) return false;

        _workshopService.Remove(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
