using Microsoft.Extensions.DependencyInjection;
using Orders.Application;

namespace Orders.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(OrdersApplicationAssembly.Reference));

        return services;
    }
}
