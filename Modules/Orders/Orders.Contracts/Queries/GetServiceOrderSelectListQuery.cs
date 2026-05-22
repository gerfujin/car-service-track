using MediatR;

namespace Orders.Contracts.Queries;

public record GetServiceOrderSelectListQuery : IRequest<IEnumerable<SelectListItemDto>>;
