using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Infrastructure.Repositories;

namespace Orders.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(OrdersApplicationAssembly.Reference));

        services.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    OrdersDbContext.SchemaName));
        });

        services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();
        services.AddScoped<IServiceOrderService, ServiceOrderService>();
        services.AddScoped<IServiceOrderPartService, ServiceOrderPartService>();
        services.AddScoped<IStatusHistoryService, StatusHistoryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IRepairPhotoService, RepairPhotoService>();

        return services;
    }
}
