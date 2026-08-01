using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Notifications;

public interface INotificationDeviceRepository
{
    Task<NotificationDevice?> GetDeviceAsync(string studentId, string deviceId);
    Task<NotificationDevice?> GetDeviceByTokenAsync(string token);
    Task AddDeviceAsync(NotificationDevice device);
    Task UpdateDeviceAsync(NotificationDevice device);
}