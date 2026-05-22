using MediatR;
using Workshops.Application.Services;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

internal sealed class GetAllSparePartsQueryHandler : IRequestHandler<GetAllSparePartsQuery, IEnumerable<SparePartDto>>
{
    private readonly ISparePartService _sparePartService;

    public GetAllSparePartsQueryHandler(ISparePartService sparePartService)
    {
        _sparePartService = sparePartService;
    }

    public async Task<IEnumerable<SparePartDto>> Handle(GetAllSparePartsQuery request, CancellationToken cancellationToken)
    {
        var parts = await _sparePartService.AllAsync();
        return parts.Select(sp => new SparePartDto(
            sp.Id, sp.Name, sp.PartNumber, sp.Country, sp.UnitPrice, sp.StockQuantity));
    }
}
