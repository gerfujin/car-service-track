using MediatR;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Commands;

namespace Orders.Application.Commands;

internal sealed class DeleteServiceOrderPartCommandHandler : IRequestHandler<DeleteServiceOrderPartCommand, bool>
{
    private readonly IServiceOrderPartService _partService;
    private readonly IOrdersUnitOfWork _uow;

    public DeleteServiceOrderPartCommandHandler(IServiceOrderPartService partService, IOrdersUnitOfWork uow)
    {
        _partService = partService;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteServiceOrderPartCommand request, CancellationToken cancellationToken)
    {
        var entity = await _partService.FindAsync(request.Id);
        if (entity == null) return false;

        var orderId = entity.ServiceOrderId;
        _partService.Remove(entity);
        await _partService.RecalculateOrderTotalAsync(orderId);
        await _uow.SaveChangesAsync();
        return true;
    }
}
