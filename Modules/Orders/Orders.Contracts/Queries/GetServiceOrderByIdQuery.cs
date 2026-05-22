using MediatR;

namespace Orders.Contracts.Queries;

public record GetServiceOrderByIdQuery(Guid OrderId) : IRequest<ServiceOrderDto?>;
