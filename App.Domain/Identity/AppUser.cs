using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace App.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IBaseEntity
{
    public ICollection<AppRefreshToken>? RefreshTokens { get; set; }

    // One user can have one Owner profile
    public Owner? Owner { get; set; }
}
