using MediatR;

namespace Workshops.Contracts.Queries;

public record GetServiceByIdQuery(Guid ServiceId) : IRequest<ServiceDto?>;
