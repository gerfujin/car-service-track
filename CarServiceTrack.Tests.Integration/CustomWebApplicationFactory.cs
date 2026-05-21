using App.DAL.EF;
using CarServiceTrack.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarServiceTrack.Tests.Integration;

/// <summary>
/// Replaces PostgreSQL with a named SQLite in-memory database so integration tests
/// never touch a real server. A keep-alive connection prevents SQLite from
/// destroying the in-memory database between EF Core DbContext lifetimes.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique per-factory database name so parallel test classes don't collide.
    private readonly string _connectionString =
        $"DataSource=cst_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // Keep this connection open for the entire lifetime of the factory so the
    // in-memory SQLite database is not destroyed between request-scoped DbContexts.
    private readonly SqliteConnection _keepAlive;

    public CustomWebApplicationFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
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

        builder.ConfigureServices(services =>
        {
            // EF Core 9/10 registers an IDbContextOptionsConfiguration<AppDbContext>
            // delegate via Add (not TryAdd) inside AddDbContext. Calling AddDbContext
            // again for SQLite would therefore ADD a second provider delegate, so both
            // Npgsql and SQLite get applied → "Only a single database provider can be
            // registered". We must strip every EF registration tied to the original
            // Npgsql AppDbContext before re-registering SQLite.
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().Name
                         .StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // Re-register AppDbContext pointing at the SQLite in-memory database.
            // Mirror the production query-tracking behavior: the real Program.cs uses
            // NoTrackingWithIdentityResolution. Without it the default (TrackAll) makes
            // FindAsync track an entity that Remove/Update then re-attaches by key,
            // throwing "another instance with the same key value is already tracked".
            services.AddDbContext<AppDbContext>(options =>
                options
                    .UseSqlite(_connectionString)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));
        });
    }

    /// <summary>
    /// Creates the schema and runs the caller-supplied seeder inside a dedicated
    /// scope, returning whatever the seeder produces (e.g. a <see cref="Fixtures.SeedResult"/>).
    /// Call once per test-class <c>IAsyncLifetime.InitializeAsync</c>.
    /// </summary>
    public async Task<T> InitializeDbAsync<T>(Func<IServiceProvider, Task<T>> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // EnsureCreated builds the schema from the EF model — no PG migrations needed.
        await db.Database.EnsureCreatedAsync();
        return await seed(scope.ServiceProvider);
    }

    /// <summary>Overload for seeders that return no value.</summary>
    public async Task InitializeDbAsync(Func<IServiceProvider, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await seed(scope.ServiceProvider);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keepAlive.Dispose();
        }
        base.Dispose(disposing);
    }
}
