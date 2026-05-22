using MediatR;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Commands;

namespace Orders.Application.Commands;

internal sealed class UpdateServiceOrderStatusAdminCommandHandler : IRequestHandler<UpdateServiceOrderStatusAdminCommand, bool>
{
    private readonly IServiceOrderService _serviceOrderService;
    private readonly IOrdersUnitOfWork _uow;

    public UpdateServiceOrderStatusAdminCommandHandler(IServiceOrderService serviceOrderService, IOrdersUnitOfWork uow)
    {
        _serviceOrderService = serviceOrderService;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateServiceOrderStatusAdminCommand request, CancellationToken cancellationToken)
    {
        var result = await _serviceOrderService.UpdateStatusForAdminAsync(
            request.OrderId, request.Status, request.MechanicId, request.FinalPrice, request.Notes);

        if (!result) return false;

        await _uow.SaveChangesAsync();
        return true;
    }
}
