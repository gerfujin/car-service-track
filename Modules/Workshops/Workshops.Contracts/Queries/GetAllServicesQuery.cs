using MediatR;

namespace Workshops.Contracts.Queries;

public record GetAllServicesQuery : IRequest<IEnumerable<ServiceDto>>;
