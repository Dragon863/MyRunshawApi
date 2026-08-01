using MyRunshaw.Workers.HostedServices.BusArrivalScraper;
using Xunit;

namespace MyRunshaw.Workers.Tests;

public class BusArrivalScraperWorkerTests
{
    [Fact]
    public void IsWithinInterval_UsesEuropeLondonTimeZone()
    {
        var result = BusArrivalScraperWorker.IsWithinInterval(
            new DateTimeOffset(2026, 7, 30, 15, 11, 0, TimeSpan.Zero),
            new TimeOnly(15, 0),
            new TimeOnly(16, 15));

        Assert.True(result);
    }

    [Fact]
    public void IsWithinInterval_ReturnsFalse_OutsideConfiguredWindow()
    {
        var result = BusArrivalScraperWorker.IsWithinInterval(
            new DateTimeOffset(2026, 7, 30, 13, 59, 0, TimeSpan.Zero),
            new TimeOnly(15, 0),
            new TimeOnly(16, 15));

        Assert.False(result);
    }
}
