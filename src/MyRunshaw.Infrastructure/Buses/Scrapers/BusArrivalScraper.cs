using HtmlAgilityPack;

namespace MyRunshaw.Infrastructure.Buses;

public sealed class BusArrivalScraper(
    HttpClient httpClient) : IBusArrivalScraper
{
    private const string Url = "https://webservices.runshaw.ac.uk/bus/busdepartures.aspx";

    public async Task<IReadOnlyList<ScrapedBusArrival>> GetArrivalsAsync(
     CancellationToken cancellationToken = default)
    {
        var html = await httpClient.GetStringAsync(Url, cancellationToken);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var tableRecords = doc.DocumentNode
            .SelectNodes("//tr")
            ?? null;

        if (tableRecords == null)
        {
            return Array.Empty<ScrapedBusArrival>();
        }

        var result = tableRecords
            .Select(a =>
            {
                // contains 3 td elements: bus number, blank/whitespace, and bus bay
                var tdElements = a.SelectNodes("td");

                if (tdElements == null || tdElements.Count < 3)
                {
                    return null;
                }

                var busNumber = tdElements[0].InnerText.Trim();
                var busBay = tdElements[2].InnerText.Trim();
                if (busBay == "&nbsp;" || busBay == "")
                {
                    busBay = "0"; // Treat &nbsp; as "0" for no bay
                }

                return new ScrapedBusArrival(busNumber, busBay);
            })
            .Where(x => x != null) // remove null entries
            .ToList();

        return result!;
    }
}

