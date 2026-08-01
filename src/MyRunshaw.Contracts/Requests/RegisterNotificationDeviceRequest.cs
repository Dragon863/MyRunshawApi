namespace MyRunshaw.Contracts.Requests;

public class RegisterNotificationDeviceRequest
{
    public string Token { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Platform { get; set; }
    public string? AppVersion { get; set; }
}
