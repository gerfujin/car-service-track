using MediatR;
using Orders.Domain.Enums;

namespace Orders.Contracts.Commands;

public record UpdateServiceOrderStatusAdminCommand(
    Guid OrderId,
    ServiceOrderStatus Status,
    Guid? MechanicId,
    decimal? FinalPrice,
    string? Notes
) : IRequest<bool>;
