using MyRunshaw.Contracts.Requests;
using MyRunshaw.Contracts.Responses;

public interface INotificationDeviceService
{
    Task RegisterDeviceAsync(string studentId, string deviceId, RegisterNotificationDeviceRequest request);
    Task<NotificationDevicePreferences?> GetPreferencesAsync(string studentId, string deviceId);
    Task UpdatePreferencesAsync(string studentId, string deviceId, UpdateNotificationDevicePreferencesRequest request);
}