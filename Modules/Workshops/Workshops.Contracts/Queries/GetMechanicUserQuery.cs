using MediatR;

namespace Workshops.Contracts.Queries;

// Cross-module query: Workshops asks Users for the identity record of a mechanic.
// AppUserId is the bare Guid FK stored on the Mechanic entity (no navigation property).
public record GetMechanicUserQuery(Guid MechanicId, Guid AppUserId) : IRequest<MechanicUserDto?>;
