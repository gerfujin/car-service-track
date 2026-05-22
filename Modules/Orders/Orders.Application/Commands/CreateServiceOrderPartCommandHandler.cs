using MediatR;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Commands;
using Orders.Contracts.Queries;
using Workshops.Contracts.Queries;

namespace Orders.Application.Commands;

internal sealed class CreateServiceOrderPartCommandHandler : IRequestHandler<CreateServiceOrderPartCommand, ServiceOrderPartDto?>
{
    private readonly IServiceOrderPartService _partService;
    private readonly IOrdersUnitOfWork _uow;
    private readonly ISender _sender;

    public CreateServiceOrderPartCommandHandler(
        IServiceOrderPartService partService,
        IOrdersUnitOfWork uow,
        ISender sender)
    {
        _partService = partService;
        _uow = uow;
        _sender = sender;
    }

    public async Task<ServiceOrderPartDto?> Handle(CreateServiceOrderPartCommand request, CancellationToken cancellationToken)
    {
        // Resolve unit price: if caller provided ≤ 0, fetch the spare part's default price.
        var unitPrice = request.Price;
        if (unitPrice <= 0)
        {
            var sparePart = await _sender.Send(new GetSparePartByIdQuery(request.SparePartId), cancellationToken);
            if (sparePart == null) return null;
            unitPrice = sparePart.UnitPrice;
        }

        var entity = new BllServiceOrderPart
        {
            ServiceOrderId = request.ServiceOrderId,
            SparePartId = request.SparePartId,
            Quantity = request.Quantity,
            UnitPrice = unitPrice
        };

        _partService.Add(entity);
        await _partService.RecalculateOrderTotalAsync(entity.ServiceOrderId);
        await _uow.SaveChangesAsync();

        return new ServiceOrderPartDto(
            entity.Id, entity.ServiceOrderId, entity.SparePartId,
            entity.SparePartName, entity.Quantity, entity.UnitPrice);
    }
}
