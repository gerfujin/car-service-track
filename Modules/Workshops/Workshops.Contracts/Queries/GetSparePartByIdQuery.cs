using MediatR;

namespace Workshops.Contracts.Queries;

public record GetSparePartByIdQuery(Guid SparePartId) : IRequest<SparePartDto?>;
