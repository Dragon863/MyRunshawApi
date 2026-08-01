namespace MyRunshaw.Contracts.Requests;

public class UpdateNotificationDevicePreferencesRequest
{
    public bool? NotificationsEnabled { get; set; }
    public bool? BusNotificationsEnabled { get; set; }
}
