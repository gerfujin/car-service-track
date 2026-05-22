using MediatR;
using Workshops.Contracts.Queries;

namespace Workshops.Contracts.Commands;

public record UpdateMechanicCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Specialization
) : IRequest<MechanicDto?>;
