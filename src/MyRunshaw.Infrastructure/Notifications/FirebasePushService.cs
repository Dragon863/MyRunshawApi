using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyRunshaw.Application.Notifications;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Notifications;

/// <summary>Firebase implementation, enabled with PushNotifications:Provider=Firebase.</summary>
public class FirebasePushService : IPushNotificationService
{
    private const int MaxMulticastTokens = 500;
    private static readonly object FirebaseAppLock = new();
    private static FirebaseApp? _firebaseApp;

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FirebasePushService> _logger;

    public FirebasePushService(AppDbContext dbContext, IConfiguration configuration, ILogger<FirebasePushService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendToUserAsync(string studentId, string heading, string content, int ttlSeconds = 600,
        string? androidChannelId = null, string smallIcon = "app_logo", int priority = 10,
        string destination = "friends", string? busId = null, string? bay = null) =>
        SendToUsersAsync([studentId], heading, content, ttlSeconds, androidChannelId, smallIcon, priority, destination, busId, bay);

    public async Task SendToUsersAsync(IEnumerable<string> studentIds, string heading, string content, int ttlSeconds = 600,
            string? androidChannelId = null, string smallIcon = "app_logo", int priority = 10,
            string destination = "friends", string? busId = null, string? bay = null)
    {
        _logger.LogInformation("Sending Firebase push to {Count} users. Heading: {Heading}, Content: {Content}, Destination: {Destination}, BusId: {BusId}, Bay: {Bay}", studentIds.Count(), heading, content, destination, busId, bay);
        var recipients = studentIds.Distinct().ToList();
        if (recipients.Count == 0) return;

        if (destination == "bus" && string.IsNullOrWhiteSpace(busId))
        {
            _logger.LogError("A bus notification was requested without a bus ID.");
            return;
        }

        // if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        // {
        //     _logger.LogInformation("Debug mode: skipping Firebase push to {Count} users.", recipients.Count);
        //     return;
        // }

        // Check which buses the student is subscribed to if this is a bus notification. If not, just send to all active devices for the student.
        var devices = await _dbContext.NotificationDevices
            .Where(d => recipients.Contains(d.StudentId)
                        && d.IsActive
                        && d.FcmToken != null
                        && d.NotificationsEnabled
                        && (destination != "bus" ||
                            (d.BusNotificationsEnabled && _dbContext.BusSubscriptions.Any(bs => bs.StudentId == d.StudentId && bs.BusId == busId))))
            .Select(d => new DeviceToken(d.Id, d.FcmToken!))
            .ToListAsync();

        if (devices.Count == 0)
        {
            _logger.LogInformation(
                "No active Firebase devices matched {Count} recipients for destination {Destination}.",
                recipients.Count,
                destination);
            return;
        }

        var data = new Dictionary<string, string> { ["destination"] = destination };
        if (!string.IsNullOrWhiteSpace(busId)) data["busId"] = busId;
        if (!string.IsNullOrWhiteSpace(bay)) data["bay"] = bay;

        var invalidDeviceIds = new List<int>();
        foreach (var batch in devices.Chunk(MaxMulticastTokens))
        {
            BatchResponse result;
            try
            {
                var message = new MulticastMessage
                {
                    // TODO: Switch Tokens to Fids once this is fixed: https://github.com/firebase/firebase-admin-dotnet/issues/530
                    Tokens = batch.Select(d => d.Token).ToList(),
                    Notification = new Notification { Title = heading, Body = content },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = priority > 5 ? Priority.High : Priority.Normal,
                        TimeToLive = TimeSpan.FromSeconds(ttlSeconds),
                        CollapseKey = !string.IsNullOrWhiteSpace(busId) ? $"bus_{busId}" : null,
                        Notification = new AndroidNotification
                        {
                            ChannelId = androidChannelId,
                            Icon = smallIcon,
                            Color = "#E63009",
                            EventTimestamp = DateTime.UtcNow, // without this it will default to 1st Jan 1AD. Thanks, firebase. That took an hour to debug.
                            Tag = !string.IsNullOrWhiteSpace(busId) ? $"bus_{busId}" : null // overwrite the existing notification in the tray
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            ["apns-expiration"] = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ttlSeconds).ToString(),
                            ["apns-priority"] = (priority > 5 ? "10" : "5")
                        }
                    }
                };

                result = await FirebaseMessaging.GetMessaging(GetFirebaseApp()).SendEachForMulticastAsync(message);

                _logger.LogInformation(
                    "Firebase returned Success={Success} Failure={Failure}",
                    result.SuccessCount,
                    result.FailureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Firebase push batch.");
                continue;
            }

            for (var index = 0; index < result.Responses.Count; index++)
            {
                var response = result.Responses[index];
                if (response.IsSuccess)
                {
                    continue;
                }

                var token = batch.ElementAt(index).Token;
                var exception = response.Exception;
                _logger.LogWarning("FCM send failed for device id {DeviceId} (token: {Token}). Exception: {Exception}", batch.ElementAt(index).Id, token, exception?.Message);
                if (exception is FirebaseMessagingException messagingException)
                {
                    _logger.LogWarning(messagingException, "FCM send failed for device id {DeviceId} (token: {Token}). ErrorCode: {ErrorCode}", batch.ElementAt(index).Id, token, messagingException.MessagingErrorCode);
                    if (messagingException.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                        messagingException.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                    {
                        invalidDeviceIds.Add(batch.ElementAt(index).Id);
                    }
                }
                else if (exception != null)
                {
                    _logger.LogWarning(exception, "FCM send failed for device id {DeviceId} (token: {Token}). Exception: {Message}", batch.ElementAt(index).Id, token, exception.Message);
                }
                else
                {
                    _logger.LogWarning("FCM send failed for device id {DeviceId} (token: {Token}) without exception information.", batch.ElementAt(index).Id, token);
                }
            }
        }


        if (invalidDeviceIds.Count > 0)
        {
            var invalidDevices = await _dbContext.NotificationDevices
                .Where(d => invalidDeviceIds.Contains(d.Id))
                .ToListAsync();
            foreach (var device in invalidDevices)
            {
                device.FcmToken = null;
                device.IsActive = false;
            }
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Disabled {Count} invalid FCM device tokens.", invalidDevices.Count);
        }
    }

    private FirebaseApp GetFirebaseApp()
    {
        lock (FirebaseAppLock)
        {
            if (_firebaseApp is not null) return _firebaseApp;

            var serviceAccountJson = _configuration["Firebase:ServiceAccountJson"];
            var credential = string.IsNullOrWhiteSpace(serviceAccountJson)
                ? GoogleCredential.GetApplicationDefault()
                : GoogleCredential.FromJson(serviceAccountJson);

            _firebaseApp = FirebaseApp.Create(new AppOptions { Credential = credential }, "myrunshaw-firebase");
            return _firebaseApp;
        }
    }

    private sealed record DeviceToken(int Id, string Token);
}