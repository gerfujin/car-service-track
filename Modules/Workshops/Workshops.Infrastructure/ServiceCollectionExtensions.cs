using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Workshops.Application;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Infrastructure.Repositories;

namespace Workshops.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkshopsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(WorkshopsApplicationAssembly.Reference));

        services.AddDbContext<WorkshopsDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    WorkshopsDbContext.SchemaName));
        });

        services.AddScoped<IWorkshopsUnitOfWork, WorkshopsUnitOfWork>();
        services.AddScoped<IWorkshopService, WorkshopService>();
        services.AddScoped<IMechanicService, MechanicService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<ISparePartService, SparePartService>();

        return services;
    }
}
