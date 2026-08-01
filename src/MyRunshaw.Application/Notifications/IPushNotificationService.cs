namespace MyRunshaw.Application.Notifications;

public interface IPushNotificationService
{
    Task SendToUserAsync(string studentId, string heading, string content, int ttlSeconds = 600, string? androidChannelId = null, string smallIcon = "app_logo", int priority = 10, string destination = "friends", string? busId = null, string? bay = null);

    Task SendToUsersAsync(IEnumerable<string> studentIds, string heading, string content, int ttlSeconds = 600, string? androidChannelId = null, string smallIcon = "app_logo", int priority = 10, string destination = "friends", string? busId = null, string? bay = null);
}
