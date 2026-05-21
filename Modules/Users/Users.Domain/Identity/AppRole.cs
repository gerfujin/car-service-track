using Base.Contracts;
using Microsoft.AspNetCore.Identity;

namespace Users.Domain.Identity;

public class AppRole : IdentityRole<Guid>, IBaseEntity { }
