using MyRunshaw.Application.Buses.Services;

namespace MyRunshaw.Workers.HostedServices.BusArrivalScraper;

public class BusArrivalScraperWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int INTERVAL_SECONDS = 10;

    private readonly TimeOnly _startTime = new TimeOnly(15, 0);  // 3:00 PM
    private readonly TimeOnly _endTime = new TimeOnly(16, 15);   // 4:15 PM

    public bool IsWithinInterval()
    {
        var now = DateTime.Now;
        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            return false;

        var timeOfDay = TimeOnly.FromDateTime(now);
        return timeOfDay >= _startTime && timeOfDay <= _endTime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (IsWithinInterval())
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<BusArrivalScraperService>();
                    await service.ScrapeAndSendPushNotificationsAsync();
                }
                catch (Exception ex)
                {
                    // don't crash the service if one scrape fails, just log it and try again next time
                    Console.WriteLine($"Scraper failed: {ex.Message}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(INTERVAL_SECONDS), stoppingToken);
        }
    }
}