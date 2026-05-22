using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class CreateSparePartCommandHandler : IRequestHandler<CreateSparePartCommand, SparePartDto>
{
    private readonly ISparePartService _sparePartService;
    private readonly IWorkshopsUnitOfWork _uow;

    public CreateSparePartCommandHandler(ISparePartService sparePartService, IWorkshopsUnitOfWork uow)
    {
        _sparePartService = sparePartService;
        _uow = uow;
    }

    public async Task<SparePartDto> Handle(CreateSparePartCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllSparePart
        {
            Name = request.Name,
            PartNumber = request.PartNumber,
            UnitPrice = request.UnitPrice,
            StockQuantity = request.StockQuantity
        };

        var added = _sparePartService.Add(bll);
        await _uow.SaveChangesAsync();

        return new SparePartDto(added.Id, added.Name, added.PartNumber, added.Country, added.UnitPrice, added.StockQuantity);
    }
}
