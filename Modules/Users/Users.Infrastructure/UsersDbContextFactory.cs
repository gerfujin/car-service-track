using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Users.Infrastructure;

public class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=carservicetrack;Username=postgres;Password=postgres",
            options => options.MigrationsHistoryTable("__EFMigrationsHistory", UsersDbContext.SchemaName));

        return new UsersDbContext(optionsBuilder.Options);
    }
}
