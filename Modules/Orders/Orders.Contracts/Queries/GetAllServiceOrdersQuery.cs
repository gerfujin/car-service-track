using MediatR;

namespace Orders.Contracts.Queries;

public record GetAllServiceOrdersQuery : IRequest<IEnumerable<ServiceOrderDto>>;
