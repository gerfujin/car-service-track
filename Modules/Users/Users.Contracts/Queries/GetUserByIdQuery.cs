using MediatR;

namespace Users.Contracts.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;
