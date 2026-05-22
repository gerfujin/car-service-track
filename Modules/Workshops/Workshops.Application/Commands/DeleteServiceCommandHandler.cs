using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;

namespace Workshops.Application.Commands;

internal sealed class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, bool>
{
    private readonly IServiceService _serviceService;
    private readonly IWorkshopsUnitOfWork _uow;

    public DeleteServiceCommandHandler(IServiceService serviceService, IWorkshopsUnitOfWork uow)
    {
        _serviceService = serviceService;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _serviceService.FindAsync(request.Id);
        if (entity == null) return false;

        _serviceService.Remove(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
