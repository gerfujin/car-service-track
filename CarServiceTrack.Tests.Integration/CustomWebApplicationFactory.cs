using CarServiceTrack.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Infrastructure;
using Users.Infrastructure;
using Workshops.Infrastructure;

namespace CarServiceTrack.Tests.Integration;

/// <summary>
/// Replaces PostgreSQL with named SQLite in-memory databases so integration tests
/// never touch a real server. Each module DbContext gets its own unique named
/// database and a keep-alive connection so the in-memory database persists across
/// EF Core DbContext lifetimes within the same test.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique per-factory database names so parallel test classes don't collide.
    private readonly string _usersConnectionString =
        $"DataSource=cst_users_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly string _workshopsConnectionString =
        $"DataSource=cst_workshops_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly string _ordersConnectionString =
        $"DataSource=cst_orders_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // Keep connections open for the entire lifetime of the factory so the
    // in-memory SQLite databases are not destroyed between request-scoped DbContexts.
    private readonly SqliteConnection _usersKeepAlive;
    private readonly SqliteConnection _workshopsKeepAlive;
    private readonly SqliteConnection _ordersKeepAlive;

    public CustomWebApplicationFactory()
    {
        _usersKeepAlive = new SqliteConnection(_usersConnectionString);
        _usersKeepAlive.Open();
        _workshopsKeepAlive = new SqliteConnection(_workshopsConnectionString);
        _workshopsKeepAlive.Open();
        _ordersKeepAlive = new SqliteConnection(_ordersConnectionString);
        _ordersKeepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Mark this as the Testing environment so Program.cs skips the
        // PostgreSQL "wait for db connection" probe entirely.
        builder.UseEnvironment("Testing");

        // Disable all data-initialization so the Migrate/Seed calls in
        // Program.cs are no-ops during tests.
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataInitialization:DropDatabase"] = "false",
                ["DataInitialization:MigrateDatabase"] = "false",
                ["DataInitialization:SeedIdentity"] = "false",
                ["DataInitialization:SeedData"] = "false",
                // Disable HTTPS-only requirement so the test client works over http.
                ["ASPNETCORE_HTTPS_PORT"] = "",
                // Provide valid JWT config for token generation in tests.
                ["JWT:Key"] = TestJwtHelper.Key,
                ["JWT:Issuer"] = TestJwtHelper.Issuer,
                ["JWT:Audience"] = TestJwtHelper.Audience,
                ["JWT:ExpiresInSeconds"] = "3600",
                ["LangStrDefaultCulture"] = "en",
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureServices(services =>
        {
            // ── Replace AppDbContext ─────────────────────────────────────────────
            // EF Core 9/10 registers IDbContextOptionsConfiguration<T> delegates
            // via Add (not TryAdd). Calling AddDbContext again would ADD a second
            // provider delegate → "Only a single database provider can be registered".
            // Strip every EF registration tied to AppDbContext before re-registering.

            // ── Replace module DbContexts ───────────────────────────────────────
            // Module DbContexts are registered by AddUsersModule / AddWorkshopsModule /
            // AddOrdersModule in Program.cs. Replace Postgres with SQLite in-memory.
            RemoveDbContextRegistrations<UsersDbContext>(services);
            services.AddDbContext<UsersDbContext>(options =>
                options
                    .UseSqlite(_usersConnectionString)
                    // UsersDbContext has FK: Owner.AppUserId → users.AspNetUsers.Id and
                    // AppRefreshToken.AppUserId → users.AspNetUsers.Id.  In tests, identity
                    // users live only in AppDbContext's SQLite (via UserManager), so these
                    // cross-DB FKs would always fail.  Disable FK enforcement on every
                    // connection so the module works the same way it will in production
                    // PostgreSQL (where the FK references a schema managed by AppDbContext).
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));

            RemoveDbContextRegistrations<WorkshopsDbContext>(services);
            services.AddDbContext<WorkshopsDbContext>(options =>
                options
                    .UseSqlite(_workshopsConnectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));

            RemoveDbContextRegistrations<OrdersDbContext>(services);
            services.AddDbContext<OrdersDbContext>(options =>
                options
                    .UseSqlite(_ordersConnectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));
        });
    }

    /// <summary>
    /// Strips all EF Core service registrations for a specific DbContext type
    /// so the context can be re-registered with a different provider.
    /// </summary>
    private static void RemoveDbContextRegistrations<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<TContext>) ||
                d.ServiceType == typeof(TContext) ||
                (d.ServiceType == typeof(DbContextOptions)) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name
                     .StartsWith("IDbContextOptionsConfiguration") &&
                 d.ServiceType.GenericTypeArguments.Length == 1 &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(TContext)))
            .ToList();

        foreach (var descriptor in toRemove)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Creates the schema for ALL DbContexts and runs the caller-supplied seeder inside
    /// a dedicated scope, returning whatever the seeder produces.
    /// Call once per test-class <c>IAsyncLifetime.InitializeAsync</c>.
    /// </summary>
    public async Task<T> InitializeDbAsync<T>(Func<IServiceProvider, Task<T>> seed)
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Create schemas for all contexts before seeding.
        await sp.GetRequiredService<UsersDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<WorkshopsDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<OrdersDbContext>().Database.EnsureCreatedAsync();

        return await seed(sp);
    }

    /// <summary>Overload for seeders that return no value.</summary>
    public async Task InitializeDbAsync(Func<IServiceProvider, Task> seed)
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<UsersDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<WorkshopsDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<OrdersDbContext>().Database.EnsureCreatedAsync();

        await seed(sp);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _usersKeepAlive.Dispose();
            _workshopsKeepAlive.Dispose();
            _ordersKeepAlive.Dispose();
        }
        base.Dispose(disposing);
    }
}
