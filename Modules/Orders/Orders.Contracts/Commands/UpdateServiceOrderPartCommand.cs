using MediatR;
using Orders.Contracts.Queries;

namespace Orders.Contracts.Commands;

/// <param name="Price">When ≤ 0, the handler uses the spare part's default unit price.</param>
public record UpdateServiceOrderPartCommand(
    Guid Id,
    Guid ServiceOrderId,
    Guid SparePartId,
    int Quantity,
    decimal Price
) : IRequest<ServiceOrderPartDto?>;
