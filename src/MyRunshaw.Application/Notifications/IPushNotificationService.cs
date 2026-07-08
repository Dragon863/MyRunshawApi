namespace MyRunshaw.Application.Notifications;

public interface IPushNotificationService
{
    Task SendToUserAsync(string studentId, string heading, string content, int ttlSeconds = 600, string? androidChannelId = null, string? smallIcon = null, int priority = 10);

    Task SendToUsersAsync(IEnumerable<string> studentIds, string heading, string content, int ttlSeconds = 600, string? androidChannelId = null, string? smallIcon = null, int priority = 10);
}