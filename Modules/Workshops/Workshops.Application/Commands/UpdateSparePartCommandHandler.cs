using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class UpdateSparePartCommandHandler : IRequestHandler<UpdateSparePartCommand, SparePartDto?>
{
    private readonly ISparePartService _sparePartService;
    private readonly IWorkshopsUnitOfWork _uow;

    public UpdateSparePartCommandHandler(ISparePartService sparePartService, IWorkshopsUnitOfWork uow)
    {
        _sparePartService = sparePartService;
        _uow = uow;
    }

    public async Task<SparePartDto?> Handle(UpdateSparePartCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllSparePart
        {
            Id = request.Id,
            Name = request.Name,
            PartNumber = request.PartNumber,
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity
        };

        var updated = await _sparePartService.UpdateAsync(bll);
        if (updated == null) return null;

        await _uow.SaveChangesAsync();

        return new SparePartDto(updated.Id, updated.Name, updated.PartNumber, updated.Country, updated.UnitPrice, updated.StockQuantity);
    }
}
