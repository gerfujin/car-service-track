using MediatR;

namespace Users.Contracts.Queries;

public record GetOwnerByAppUserIdQuery(Guid AppUserId) : IRequest<OwnerDto?>;
