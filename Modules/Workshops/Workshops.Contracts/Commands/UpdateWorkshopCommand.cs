using MediatR;
using Workshops.Contracts.Queries;

namespace Workshops.Contracts.Commands;

public record UpdateWorkshopCommand(
    Guid Id,
    string Name,
    string Address,
    string? Phone,
    string? Email
) : IRequest<WorkshopDto?>;
