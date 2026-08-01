using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyRunshaw.Application.Notifications;

namespace MyRunshaw.Application.Buses.Services;

public sealed class BusArrivalScraperService
{
    private readonly IBusArrivalScraper _scraper;
    private readonly IBusRepository _repository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly string _busNotificationChannelId;
    private readonly ILogger<BusArrivalScraperService> _logger;

    public BusArrivalScraperService(
        IBusArrivalScraper scraper,
        IBusRepository repository,
        IPushNotificationService pushNotificationService,
        IConfiguration configuration,
        ILogger<BusArrivalScraperService> logger)
    {
        _scraper = scraper;
        _repository = repository;
        _pushNotificationService = pushNotificationService;
        _busNotificationChannelId = configuration["NotificationChannels:Bus:Id"] ?? throw new ArgumentNullException("BusNotificationChannelId is not configured.");
        _logger = logger;
    }

    public async Task ScrapeAndSendPushNotificationsAsync()
    {
        var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        var ukTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ukTimeZone);

        // Fetch scraped data and our database state
        IReadOnlyList<ScrapedBusArrival> scrapedArrivals = await _scraper.GetArrivalsAsync();
        var allBuses = await _repository.GetAllBusesAsync();

        foreach (var arrival in scrapedArrivals.DistinctBy(a => a.BusNumber))
        {
            // Find the corresponding bus in our database
            var bus = allBuses.FirstOrDefault(b => b.BusId == arrival.BusNumber);

            if (bus is null)
            {
                _logger.LogError("Bus route not found in database: {BusNumber}", arrival.BusNumber);
                continue;
            }

            var oldBay = bus.CurrentBay;
            var newBay = arrival.BusBay;

            // Bay changed?
            if (oldBay != newBay)
            {
                _logger.LogInformation("Bus {Route} changed from bay {OldBay} to bay {NewBay}", bus.BusId, oldBay, newBay);

                // Update the bus entity directly
                bus.CurrentBay = newBay;
                bus.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateBusAsync(bus);

                // Notify users!
                if (newBay != "0" && newBay != null && newBay != "") // edge cases; shouldn't happen but we'll be safe
                {
                    string message = oldBay == "0"
                        ? $"Arrived in {newBay}"
                        : $"Moved from bay {oldBay} to {newBay}";

                    await SendNotification(bus.BusId, newBay, message);
                }
            }
        }
    }

    private async Task SendNotification(string busId, string bay, string content)
    {
        var studentIds = await _repository.GetSubscribersForBusAsync(busId);

        if (!studentIds.Any()) return;

        _logger.LogInformation("Sending push to {Count} students for bus {Route}", studentIds.Count, busId);
        _logger.LogInformation("Notification content: {Content}", content);

        await _pushNotificationService.SendToUsersAsync(
            studentIds,
            heading: $"{busId} Bus Updated!",
            content: content,
            priority: 10,
            ttlSeconds: 75 * 60, // 75 minutes; helpful if buses arrive early but a student is in an exam -> phone off
            smallIcon: "ic_stat_onesignal_default",
            androidChannelId: _busNotificationChannelId,
            destination: "bus",
            busId: busId,
            bay: bay
        );
    }
}
