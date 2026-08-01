using MyRunshaw.Application.Notifications;
using MyRunshaw.Contracts.Requests;
using MyRunshaw.Contracts.Responses;
using MyRunshaw.Domain.Entities;

public class NotificationDeviceService : INotificationDeviceService
{
    private readonly INotificationDeviceRepository _repository;

    public NotificationDeviceService(INotificationDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task RegisterDeviceAsync(string studentId, string deviceId, RegisterNotificationDeviceRequest request)
    {
        var existingDevice = await _repository.GetDeviceAsync(studentId, deviceId);
        var existingTokenDevice = await _repository.GetDeviceByTokenAsync(request.Token);

        if (existingTokenDevice is not null && existingTokenDevice.Id != existingDevice?.Id)
        {
            existingTokenDevice.FcmToken = null;
            existingTokenDevice.IsActive = false;
            await _repository.UpdateDeviceAsync(existingTokenDevice);
        }

        if (existingDevice is null)
        {
            var newDevice = new NotificationDevice
            {
                StudentId = studentId,
                DeviceId = deviceId,
                FcmToken = request.Token,
                Name = request.Name,
                Platform = request.Platform,
                AppVersion = request.AppVersion,
                IsActive = true,
                LastSeenAt = DateTime.UtcNow
            };

            await _repository.AddDeviceAsync(newDevice);
            return;
        }

        existingDevice.FcmToken = request.Token;
        existingDevice.Name = request.Name;
        existingDevice.Platform = request.Platform;
        existingDevice.AppVersion = request.AppVersion;
        existingDevice.IsActive = true;
        existingDevice.LastSeenAt = DateTime.UtcNow;

        await _repository.UpdateDeviceAsync(existingDevice);
    }

    public async Task<NotificationDevicePreferences?> GetPreferencesAsync(string studentId, string deviceId)
    {
        var device = await _repository.GetDeviceAsync(studentId, deviceId);
        if (device is null)
        {
            return null;
        }

        return new NotificationDevicePreferences
        {
            NotificationsEnabled = device.NotificationsEnabled,
            BusNotificationsEnabled = device.BusNotificationsEnabled
        };
    }

    public async Task UpdatePreferencesAsync(string studentId, string deviceId, UpdateNotificationDevicePreferencesRequest request)
    {
        var device = await _repository.GetDeviceAsync(studentId, deviceId)
            ?? throw new KeyNotFoundException("Notification device not found.");

        if (request.NotificationsEnabled.HasValue)
        {
            device.NotificationsEnabled = request.NotificationsEnabled.Value;
        }

        if (request.BusNotificationsEnabled.HasValue)
        {
            device.BusNotificationsEnabled = request.BusNotificationsEnabled.Value;
        }

        device.LastSeenAt = DateTime.UtcNow;
        await _repository.UpdateDeviceAsync(device);
    }
}
