using MediatR;

namespace Workshops.Contracts.Queries;

public record GetAllSparePartsQuery : IRequest<IEnumerable<SparePartDto>>;
