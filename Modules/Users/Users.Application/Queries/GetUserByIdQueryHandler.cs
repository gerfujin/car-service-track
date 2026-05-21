using MediatR;
using Users.Application.Services;
using Users.Contracts.Queries;

namespace Users.Application.Queries;

internal sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IAppUserService _appUserService;

    public GetUserByIdQueryHandler(IAppUserService appUserService)
    {
        _appUserService = appUserService;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _appUserService.FindWithOwnerAsync(request.UserId);
        if (user is null)
            return null;

        return new UserDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName);
    }
}
