using System.Reflection;

namespace Users.Application;

// Public marker used by Users.Infrastructure to locate handler assemblies for MediatR registration.
public static class UsersApplicationAssembly
{
    public static readonly Assembly Reference = typeof(UsersApplicationAssembly).Assembly;
}
