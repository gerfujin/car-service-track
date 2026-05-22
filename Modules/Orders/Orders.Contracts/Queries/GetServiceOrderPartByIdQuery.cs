using MediatR;

namespace Orders.Contracts.Queries;

public record GetServiceOrderPartByIdQuery(Guid Id) : IRequest<ServiceOrderPartDto?>;
