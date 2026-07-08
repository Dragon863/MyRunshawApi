using HtmlAgilityPack;

namespace MyRunshaw.Infrastructure.Buses;

public sealed class BusRouteScraper(
    HttpClient httpClient) : IBusRouteScraper
{
    private const string Url =
        "https://runshaw.ac.uk/life-at-runshaw/student-services/transport/bus-route-maps/";

    public async Task<IReadOnlyList<ScrapedBusRoute>> GetRoutesAsync(
     CancellationToken cancellationToken = default)
    {
        var html = await httpClient.GetStringAsync(Url, cancellationToken);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode
            .SelectNodes("//a[contains(@href, 'google.com/maps')]")
            ?? null;

        if (links == null)
        {
            return Array.Empty<ScrapedBusRoute>();
        }

        var result = links
            .Select(a =>
            {
                var href = a.GetAttributeValue("href", "");
                var text = a.InnerText.Trim();

                return new ScrapedBusRoute(text, href);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.BusNumber))
            .ToList();

        return result;
    }
}

