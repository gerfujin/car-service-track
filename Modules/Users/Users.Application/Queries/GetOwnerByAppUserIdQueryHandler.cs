using MediatR;
using Users.Application.Services;
using Users.Contracts.Queries;

namespace Users.Application.Queries;

internal sealed class GetOwnerByAppUserIdQueryHandler : IRequestHandler<GetOwnerByAppUserIdQuery, OwnerDto?>
{
    private readonly IOwnerService _ownerService;

    public GetOwnerByAppUserIdQueryHandler(IOwnerService ownerService)
    {
        _ownerService = ownerService;
    }

    public async Task<OwnerDto?> Handle(GetOwnerByAppUserIdQuery request, CancellationToken cancellationToken)
    {
        var owner = await _ownerService.FindByUserAsync(request.AppUserId);
        if (owner == null)
            return null;

        return new OwnerDto(
            owner.Id,
            owner.FirstName,
            owner.LastName,
            owner.Address,
            owner.Phone,
            owner.AppUserId
        );
    }
}
