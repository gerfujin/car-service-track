using MediatR;
using Workshops.Contracts.Queries;

namespace Workshops.Contracts.Commands;

public record UpdateServiceCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int EstimatedTimeMinutes
) : IRequest<ServiceDto?>;
