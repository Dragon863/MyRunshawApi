using Microsoft.Extensions.DependencyInjection;
using MyRunshaw.Application.Buses.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<BusArrivalScraperService>();
        services.AddScoped<BusRouteScraperService>();


        return services;
    }


}