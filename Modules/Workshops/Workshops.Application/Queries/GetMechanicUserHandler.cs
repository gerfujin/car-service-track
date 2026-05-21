using MediatR;
using Users.Contracts.Queries;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Queries;

// Demonstrates the cross-module call pattern:
//   - Workshops.Application references ONLY Users.Contracts (not Users.Domain / Application / Infrastructure).
//   - The actual user resolution is dispatched to the Users module handler via ISender.
internal sealed class GetMechanicUserHandler : IRequestHandler<GetMechanicUserQuery, MechanicUserDto?>
{
    private readonly ISender _sender;

    public GetMechanicUserHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<MechanicUserDto?> Handle(GetMechanicUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _sender.Send(new GetUserByIdQuery(request.AppUserId), cancellationToken);
        if (user is null)
            return null;

        return new MechanicUserDto(request.MechanicId, user.Id, user.Email);
    }
}
