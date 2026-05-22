using MediatR;

namespace Workshops.Contracts.Queries;

public record GetAllMechanicsQuery : IRequest<IEnumerable<MechanicDto>>;
