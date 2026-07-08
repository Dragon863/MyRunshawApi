using Microsoft.Extensions.Logging;
using MyRunshaw.Application.Buses;
using MyRunshaw.Application.Buses.Services;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Buses.Services;

public sealed class BusRouteScraperService(
    IBusRouteScraper scraper,
    IBusRepository repository,
    HttpClient httpClient,
    ILogger<BusRouteScraperService> logger)
{
    public async Task RefreshAsync()
    {
        logger.LogInformation("Starting weekly bus route sync...");

        var routes = await scraper.GetRoutesAsync();

        var grouped = routes.GroupBy(r => r.GoogleMapsUrl);

        logger.LogInformation("Found {Count} unique routes to process.", grouped.Count());

        foreach (var group in grouped)
        {
            var routeName = group.First().BusNumber;
            var url = group.Key;

            try
            {
                var bus = await repository.GetBusByRouteNameAsync(routeName);

                if (bus is null)
                {
                    logger.LogInformation("Creating new bus route: {RouteName}", routeName);
                    bus = new Bus
                    {
                        BusId = routeName,
                        MapsRouteUrl = url
                    };
                    await repository.AddBusAsync(bus);
                }
                else
                {
                    logger.LogDebug("Bus route {RouteName} already exists. Updating...", routeName);
                    bus.MapsRouteUrl = url;
                    await repository.UpdateBusAsync(bus);
                }

                if (string.IsNullOrEmpty(url) || !url.Contains("mid="))
                {
                    logger.LogWarning("Invalid Google Maps URL for route {RouteName}: {Url}", routeName, url);
                    continue;
                }

                var mid = url.Split("mid=").Last().Split('&').First();
                var kmlUrl = $"https://www.google.com/maps/d/u/0/kml?mid={mid}&resourcekey&forcekml=1";

                logger.LogDebug("Downloading KML for {RouteName}...", routeName);
                var kmlContent = await httpClient.GetStringAsync(kmlUrl);

                var parser = new KmlParserService();
                var parsedData = parser.ParseKml(kmlContent);

                logger.LogInformation("Successfully parsed {RouteName}: Found {StopCount} stops. Description: {RouteDescription}", routeName, parsedData.Stops.Count, parsedData.RouteDescription);

                if (parsedData.Stops.Any())
                {
                    await repository.ReplaceBusStopsAsync(bus.BusId, parsedData.Stops);
                }
                // Update bus RouteDescription
                bus.RouteDescription = parsedData.RouteDescription;
                await repository.UpdateBusAsync(bus);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to download KML for route {RouteName}", routeName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while processing route {RouteName}", routeName);
            }
        }

        logger.LogInformation("Weekly bus route sync completed successfully.");
    }
}