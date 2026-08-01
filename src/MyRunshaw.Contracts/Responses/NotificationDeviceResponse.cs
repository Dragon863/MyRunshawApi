namespace MyRunshaw.Contracts.Responses;

public class NotificationDeviceResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Platform { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool BusNotificationsEnabled { get; set; }
}
