public interface IBusArrivalScraper
{
    Task<IReadOnlyList<ScrapedBusArrival>> GetArrivalsAsync(
        CancellationToken cancellationToken = default);
}

public record ScrapedBusArrival(
    string BusNumber,
    string BusBay
);