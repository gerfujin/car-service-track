using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Orders.Infrastructure;
using Users.Infrastructure;
using Users.Domain.Identity;
using Workshops.Infrastructure;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Modular monolith: each module registers its own MediatR handlers and services.
builder.Services.AddUsersModule();
builder.Services.AddWorkshopsModule();
builder.Services.AddOrdersModule();


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// using Microsoft.AspNetCore.DataProtection;
builder.Services
    .AddDataProtection();

builder.Services.AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddDefaultUI()
    .AddEntityFrameworkStores<UsersDbContext>()
    .AddDefaultTokenProviders();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
builder.Services
    .AddAuthentication()
    .AddCookie(options => { options.SlidingExpiration = true; })
    .AddJwtBearer(cfg =>
    {
        cfg.RequireHttpsMetadata = builder.Configuration.GetValue("JWT:RequireHttpsMetadata", true);
        cfg.SaveToken = true;
        cfg.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),
            ClockSkew = TimeSpan.Zero // remove delay of token when expire
        };
    });



var supportedCultures = builder.Configuration
    .GetSection("SupportedCultures")
    .GetChildren()
    .Select(x => new CultureInfo(x.Value!))
    .ToArray();

// Set LangStr default culture from config
Base.Domain.LangStr.DefaultCulture = builder.Configuration.GetValue<string>("LangStrDefaultCulture") ?? "en";

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // datetime and currency support
    options.SupportedCultures = supportedCultures;
    // UI translated strings
    options.SupportedUICultures = supportedCultures;
    // if nothing is found, use this
    options.DefaultRequestCulture = new RequestCulture("en", "en");
    options.SetDefaultCulture("en");

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        // Order is important, it's in which order they will be evaluated
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider()
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Partitioned per client IP so one caller can't exhaust the window for everyone else.
    options.AddPolicy("auth", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddCors(options =>
{
    var localFrontendOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?.Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? Array.Empty<string>();

    options.AddPolicy("CorsAllowAll", policy =>
    {
        if (localFrontendOrigins.Length > 0)
        {
            policy
                .WithOrigins(localFrontendOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("X-Version", "X-Version-Created-At");
            return;
        }

        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Version", "X-Version-Created-At");
    });
});


var apiVersioningBuilder = builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    // in case of no explicit version
    options.DefaultApiVersion = new ApiVersion(1, 0);
});

apiVersioningBuilder.AddApiExplorer(options =>
{
    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
    // note: the specified format code will format the version as "'v'major[.minor][-status]"
    options.GroupNameFormat = "'v'VVV";

    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
    // can also be used to control the format of the API version in route templates
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(c =>
{
    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddLocalization(options => options.ResourcesPath = "");

builder.Services.AddControllersWithViews()
    // Modular monolith: controllers live in each module's Presentation assembly.
    .AddApplicationPart(typeof(Users.Presentation.ApiControllers.ProfileController).Assembly)
    .AddApplicationPart(typeof(Workshops.Presentation.ApiControllers.MechanicsController).Assembly)
    .AddApplicationPart(typeof(Orders.Presentation.ApiControllers.PaymentsController).Assembly)
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ==============================================
var app = builder.Build();
// ============================================== PIPELINE ===============================
SetupAppData(app, app.Environment, app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // Baseline security response headers.
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
}

app.UseHttpsRedirection();

app.UseRequestLocalization(options: app.Services
    .GetService<IOptions<RequestLocalizationOptions>>()!.Value);

app.UseCors("CorsAllowAll");

app.UseRouting();

app.UseRateLimiter();

app.UseStaticFiles(); // Required to serve files from wwwroot (e.g. /uploads/repair-photos/)

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            description.GroupName.ToUpperInvariant()
        );
    }
    // serve from root
    // options.RoutePrefix = string.Empty;
});


app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();

return;

static void SetupAppData(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
{
    using var serviceScope = ((IApplicationBuilder)app).ApplicationServices
        .GetRequiredService<IServiceScopeFactory>()
        .CreateScope();
    var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();

    using var usersContext = serviceScope.ServiceProvider.GetRequiredService<UsersDbContext>();
    using var workshopsContext = serviceScope.ServiceProvider.GetRequiredService<WorkshopsDbContext>();
    using var ordersContext = serviceScope.ServiceProvider.GetRequiredService<OrdersDbContext>();

    // Integration tests run against an in-memory SQLite database and replace the
    // DbContext registration, so there is no PostgreSQL server to wait for.
    if (!env.IsEnvironment("Testing"))
    {
        WaitDbConnection(usersContext, logger);
    }

    using var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    using var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

    if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
    {
        logger.LogWarning("DropDatabase");
        usersContext.Database.EnsureDeleted();
        workshopsContext.Database.EnsureDeleted();
        ordersContext.Database.EnsureDeleted();
    }

    if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
    {
        logger.LogInformation("MigrateDatabase: users schema");
        usersContext.Database.Migrate();
        logger.LogInformation("MigrateDatabase: workshops schema");
        workshopsContext.Database.Migrate();
        logger.LogInformation("MigrateDatabase: orders schema");
        ordersContext.Database.Migrate();
    }

    if (configuration.GetValue<bool>("DataInitialization:SeedIdentity"))
    {
        logger.LogInformation("SeedIdentity");
        SeedIdentity(userManager, roleManager, logger);
    }

    if (configuration.GetValue<bool>("DataInitialization:SeedData"))
    {
        logger.LogInformation("SeedData: workshops module");
        SeedWorkshopsData(workshopsContext, logger);
    }
}

static void WaitDbConnection(UsersDbContext ctx, ILogger logger)
{
    while (true)
    {
        try
        {
            ctx.Database.OpenConnection();
            ctx.Database.CloseConnection();
            return;
        }
        catch (Exception e)
        {
            logger.LogWarning("Checked postgres db connection. Got: {}", e.Message);

            if (e.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Applying migration, probably db is not there (but server is)");
                return;
            }

            if (IsNonRecoverableConnectionError(e))
            {
                logger.LogError(e, "Non-recoverable database connection error. Stopping retries.");
                throw;
            }

            logger.LogWarning("Waiting for db connection. Sleep 1 sec");
            System.Threading.Thread.Sleep(1000);
        }
    }
}

static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, ILogger logger)
{
    foreach (var roleName in new[] { "admin", "client", "mechanic" })
    {
        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
        {
            var createRole = roleManager.CreateAsync(new AppRole { Name = roleName }).GetAwaiter().GetResult();
            if (!createRole.Succeeded)
            {
                logger.LogWarning("Failed to create role {Role}: {Errors}",
                    roleName,
                    string.Join(", ", createRole.Errors.Select(e => e.Description)));
            }
        }
    }

    // Seed default users (matching A4 InitialData)
    var seedUsers = new[]
    {
        (email: "admin@carservice.ee",    password: "Admin.12345",  role: "admin"),
        (email: "mechanic@carservice.ee", password: "Mech.12345",   role: "mechanic"),
        (email: "client@carservice.ee",   password: "Client.12345", role: "client"),
    };

    foreach (var (email, password, role) in seedUsers)
    {
        var existing = userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
        if (existing != null) continue;

        var user = new AppUser { Email = email, UserName = email, EmailConfirmed = true };
        var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to create seed user {Email}: {Errors}",
                email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            continue;
        }

        var roleResult = userManager.AddToRoleAsync(user, role).GetAwaiter().GetResult();
        if (!roleResult.Succeeded)
        {
            logger.LogWarning("Failed to assign role {Role} to {Email}: {Errors}",
                role, email,
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
        else
        {
            logger.LogInformation("Seeded user {Email} with role {Role}", email, role);
        }
    }
}

static void SeedWorkshopsData(WorkshopsDbContext context, ILogger logger)
{
    if (!context.Workshops.Any())
    {
        context.Workshops.AddRange(
            new Workshops.Domain.Workshop
            {
                Name = new Base.Domain.LangStr("AutoFix Tallinn", "en"),
                Address = new Base.Domain.LangStr("Pärnu mnt 12, Tallinn", "en"),
                Phone = "+372 5555 1111",
                Email = "info@autofix.ee"
            },
            new Workshops.Domain.Workshop
            {
                Name = new Base.Domain.LangStr("SpeedGarage Tartu", "en"),
                Address = new Base.Domain.LangStr("Riia 15, Tartu", "en"),
                Phone = "+372 5555 2222",
                Email = "info@speedgarage.ee"
            }
        );
        context.SaveChanges();
        logger.LogInformation("Seeded workshops");
    }

    if (!context.Services.Any())
    {
        context.Services.AddRange(
            new Workshops.Domain.Service
            {
                Name = new Base.Domain.LangStr("Oil Change", "en"),
                Description = new Base.Domain.LangStr("Full synthetic oil change with filter replacement", "en"),
                BasePrice = 49.99m
            },
            new Workshops.Domain.Service
            {
                Name = new Base.Domain.LangStr("Brake Inspection", "en"),
                Description = new Base.Domain.LangStr("Complete brake system inspection and adjustment", "en"),
                BasePrice = 39.99m
            },
            new Workshops.Domain.Service
            {
                Name = new Base.Domain.LangStr("Tire Rotation", "en"),
                Description = new Base.Domain.LangStr("Rotate all four tires for even wear", "en"),
                BasePrice = 29.99m
            },
            new Workshops.Domain.Service
            {
                Name = new Base.Domain.LangStr("Engine Diagnostics", "en"),
                Description = new Base.Domain.LangStr("Full computer diagnostics scan", "en"),
                BasePrice = 59.99m
            },
            new Workshops.Domain.Service
            {
                Name = new Base.Domain.LangStr("Air Filter Replacement", "en"),
                Description = new Base.Domain.LangStr("Replace engine air filter", "en"),
                BasePrice = 24.99m
            }
        );
        context.SaveChanges();
        logger.LogInformation("Seeded services");
    }

    if (!context.SpareParts.Any())
    {
        context.SpareParts.AddRange(
            new Workshops.Domain.SparePart
            {
                Name = new Base.Domain.LangStr("Oil Filter", "en"),
                PartNumber = "OF-001",
                UnitPrice = 12.99m,
                StockQuantity = 50
            },
            new Workshops.Domain.SparePart
            {
                Name = new Base.Domain.LangStr("Brake Pad Set (Front)", "en"),
                PartNumber = "BP-F-001",
                UnitPrice = 45.99m,
                StockQuantity = 20
            },
            new Workshops.Domain.SparePart
            {
                Name = new Base.Domain.LangStr("Air Filter", "en"),
                PartNumber = "AF-001",
                UnitPrice = 18.99m,
                StockQuantity = 30
            },
            new Workshops.Domain.SparePart
            {
                Name = new Base.Domain.LangStr("Spark Plug Set", "en"),
                PartNumber = "SP-001",
                UnitPrice = 32.99m,
                StockQuantity = 40
            },
            new Workshops.Domain.SparePart
            {
                Name = new Base.Domain.LangStr("Windshield Wiper Blades", "en"),
                PartNumber = "WW-001",
                UnitPrice = 22.99m,
                StockQuantity = 25
            }
        );
        context.SaveChanges();
        logger.LogInformation("Seeded spare parts");
    }
}

static bool IsNonRecoverableConnectionError(Exception exception)
{
    Exception? current = exception;
    while (current != null)
    {
        if (current is ArgumentException or FormatException)
        {
            return true;
        }

        if (current is NpgsqlException npgsqlException)
        {
            var message = npgsqlException.Message;
            if (message.Contains("Invalid host", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("tcp://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        current = current.InnerException;
    }

    return false;
}

// Required so that WebApplicationFactory<Program> in integration tests can see the Program class.
public partial class Program { }
