using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyRunshaw.Application.Buses;
using MyRunshaw.Infrastructure.Buses;
using MyRunshaw.Infrastructure.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHttpClient<IBusRouteScraper, BusRouteScraper>();
        services.AddHttpClient<IBusArrivalScraper, BusArrivalScraper>();

        services.AddScoped<IBusRepository, BusRepository>();

        return services;
    }
}