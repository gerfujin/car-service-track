using MediatR;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Commands;
using Orders.Contracts.Queries;
using Workshops.Contracts.Queries;

namespace Orders.Application.Commands;

internal sealed class UpdateServiceOrderPartCommandHandler : IRequestHandler<UpdateServiceOrderPartCommand, ServiceOrderPartDto?>
{
    private readonly IServiceOrderPartService _partService;
    private readonly IOrdersUnitOfWork _uow;
    private readonly ISender _sender;

    public UpdateServiceOrderPartCommandHandler(
        IServiceOrderPartService partService,
        IOrdersUnitOfWork uow,
        ISender sender)
    {
        _partService = partService;
        _uow = uow;
        _sender = sender;
    }

    public async Task<ServiceOrderPartDto?> Handle(UpdateServiceOrderPartCommand request, CancellationToken cancellationToken)
    {
        var unitPrice = request.Price;
        if (unitPrice <= 0)
        {
            var sparePart = await _sender.Send(new GetSparePartByIdQuery(request.SparePartId), cancellationToken);
            if (sparePart == null) return null;
            unitPrice = sparePart.UnitPrice;
        }

        var bll = new BllServiceOrderPart
        {
            Id = request.Id,
            ServiceOrderId = request.ServiceOrderId,
            SparePartId = request.SparePartId,
            Quantity = request.Quantity,
            UnitPrice = unitPrice
        };

        var updated = await _partService.UpdateAsync(bll);
        if (updated == null) return null;

        await _partService.RecalculateOrderTotalAsync(request.ServiceOrderId);
        await _uow.SaveChangesAsync();

        return new ServiceOrderPartDto(
            updated.Id, updated.ServiceOrderId, updated.SparePartId,
            updated.SparePartName, updated.Quantity, updated.UnitPrice);
    }
}
