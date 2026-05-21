using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Workshops.Infrastructure;

public class WorkshopsDbContextFactory : IDesignTimeDbContextFactory<WorkshopsDbContext>
{
    public WorkshopsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkshopsDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=carservicetrack;Username=postgres;Password=postgres",
            options => options.MigrationsHistoryTable("__EFMigrationsHistory", WorkshopsDbContext.SchemaName));

        return new WorkshopsDbContext(optionsBuilder.Options);
    }
}
