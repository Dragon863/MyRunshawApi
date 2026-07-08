public interface IBusRouteScraper
{
    Task<IReadOnlyList<ScrapedBusRoute>> GetRoutesAsync(
        CancellationToken cancellationToken = default);
}

public record ScrapedBusRoute(
    // this is the only info provided by the website - we'll match it afterwards.
    string BusNumber,
    string GoogleMapsUrl
);