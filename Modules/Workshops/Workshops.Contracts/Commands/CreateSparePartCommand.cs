using MediatR;
using Workshops.Contracts.Queries;

namespace Workshops.Contracts.Commands;

public record CreateSparePartCommand(
    string Name,
    string? PartNumber,
    decimal UnitPrice,
    int StockQuantity
) : IRequest<SparePartDto>;
