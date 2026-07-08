using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyRunshaw.Application.Notifications;
using OneSignalApi.Api;
using OneSignalApi.Client;
using OneSignalApi.Model;

namespace MyRunshaw.Infrastructure.Notifications;

public class OneSignalPushService : IPushNotificationService
{
    private readonly DefaultApi _oneSignalApi;
    private readonly string _appId;
    private readonly ILogger<OneSignalPushService> _logger;

    public OneSignalPushService(IConfiguration config, ILogger<OneSignalPushService> logger)
    {
        _logger = logger;
        _appId = config["OneSignal:AppId"]!;

        var appConfig = new Configuration
        {
            BasePath = "https://onesignal.com/api/v1",
            AccessToken = config["OneSignal:RestApiKey"]
        };
        _oneSignalApi = new DefaultApi(appConfig);
    }

    public async Task SendToUserAsync(string studentId, string heading, string content, int ttlSeconds = 600, string? androidChannelId = null, string? smallIcon = null, int priority = 10)
    {
        var notification = new Notification(appId: _appId)
        {
            AppId = _appId,
            Headings = new LanguageStringMap(en: heading),
            Contents = new LanguageStringMap(en: content),
            TargetChannel = Notification.TargetChannelEnum.Push,
            IncludeAliases = new Dictionary<string, List<string>>
                {
                    { "external_id", new List<string> { studentId.ToLowerInvariant() } }
                },
            Ttl = ttlSeconds,
            AndroidChannelId = androidChannelId,
            AndroidAccentColor = "FFE63009",
            SmallIcon = smallIcon,
            IsAndroid = true,
            IsIos = true,
            Priority = priority,
        };

        try
        {
            // Don't send in debug mode
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                _logger.LogInformation("Debug mode: Skipping push notification to {StudentId}: {Heading}", studentId, heading);
                return;
            }
            await _oneSignalApi.CreateNotificationAsync(notification);
            _logger.LogInformation("Push sent to {StudentId}: {Heading}", studentId, heading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to {StudentId}", studentId);
        }
    }

    public async Task SendToUsersAsync(IEnumerable<string> studentIds, string heading, string content, int ttlSeconds = 600, string? androidChannelId = null, string? smallIcon = null, int priority = 10)
    {
        var idList = studentIds.Select(id => id.ToLowerInvariant()).ToList();

        if (!idList.Any())
        {
            _logger.LogWarning("No student IDs provided for push notification.");
            return;
        }
        if (idList.Count > 2000)
        {
            // OneSignal can take up to 2000 external IDs here. That's fine for us generally; that would be a very big bus!
            // It's good practise to log this anyway just in case.
            _logger.LogError("OneSignal has a limit of 2000 aliases per notification. Provided {Count} student IDs.", idList.Count);
            idList = idList.Take(2000).ToList();
        }

        var notification = new Notification(appId: _appId)
        {
            AppId = _appId,
            Headings = new LanguageStringMap(en: heading),
            Contents = new LanguageStringMap(en: content),
            TargetChannel = Notification.TargetChannelEnum.Push,
            IncludeAliases = new Dictionary<string, List<string>>
                {
                    { "external_id", idList }
                },
            Ttl = ttlSeconds,
            AndroidChannelId = androidChannelId,
            AndroidAccentColor = "FFE63009",
            SmallIcon = smallIcon,
            IsAndroid = true,
            IsIos = true,
            Priority = priority,
        };

        try
        {
            // Don't send in debug mode
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                _logger.LogInformation("Debug mode: Skipping push notification to {StudentIds}: {Heading}", studentIds, heading);
                return;
            }
            await _oneSignalApi.CreateNotificationAsync(notification);
            _logger.LogInformation("Batch push sent to users: {Heading}", heading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to users");
        }
    }
}