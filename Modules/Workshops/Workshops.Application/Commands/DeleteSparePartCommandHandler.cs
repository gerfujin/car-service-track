using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;

namespace Workshops.Application.Commands;

internal sealed class DeleteSparePartCommandHandler : IRequestHandler<DeleteSparePartCommand, bool>
{
    private readonly ISparePartService _sparePartService;
    private readonly IWorkshopsUnitOfWork _uow;

    public DeleteSparePartCommandHandler(ISparePartService sparePartService, IWorkshopsUnitOfWork uow)
    {
        _sparePartService = sparePartService;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteSparePartCommand request, CancellationToken cancellationToken)
    {
        var entity = await _sparePartService.FindAsync(request.Id);
        if (entity == null) return false;

        _sparePartService.Remove(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
