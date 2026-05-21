using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace Users.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public ICollection<AppRefreshToken>? RefreshTokens { get; set; }
    public Owner? Owner { get; set; }
}
