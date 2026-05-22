using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Infrastructure;

public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=carservicetrack;Username=postgres;Password=postgres",
            options => options.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.SchemaName));

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
