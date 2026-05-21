using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetSparePartByIdQueryHandler : IRequestHandler<GetSparePartByIdQuery, SparePartDto?>
{
    private readonly ISparePartService _sparePartService;

    public GetSparePartByIdQueryHandler(ISparePartService sparePartService)
    {
        _sparePartService = sparePartService;
    }

    public async Task<SparePartDto?> Handle(GetSparePartByIdQuery request, CancellationToken cancellationToken)
    {
        var sparePart = await _sparePartService.FindAsync(request.SparePartId);
        if (sparePart == null)
            return null;

        return new SparePartDto(
            sparePart.Id,
            sparePart.Name,
            sparePart.PartNumber,
            sparePart.Country,
            sparePart.UnitPrice,
            sparePart.StockQuantity
        );
    }
}
