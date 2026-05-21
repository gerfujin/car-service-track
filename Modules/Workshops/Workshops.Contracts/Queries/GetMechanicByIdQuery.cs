using MediatR;

namespace Workshops.Contracts.Queries;

public record GetMechanicByIdQuery(Guid MechanicId) : IRequest<MechanicDto?>;
