using MediatR;

namespace Orders.Contracts.Queries;

/// <param name="ServiceOrderId">When non-null, filter parts for this order only.</param>
public record GetAllServiceOrderPartsQuery(Guid? ServiceOrderId = null) : IRequest<IEnumerable<ServiceOrderPartDto>>;
