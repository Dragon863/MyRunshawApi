using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Notifications;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Repositories;

public class NotificationDeviceRepository : INotificationDeviceRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationDeviceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationDevice?> GetDeviceAsync(string studentId, string deviceId)
    {
        return await _dbContext.NotificationDevices
            .FirstOrDefaultAsync(d => d.StudentId == studentId && d.DeviceId == deviceId);
    }

    public async Task<NotificationDevice?> GetDeviceByTokenAsync(string token)
    {
        return await _dbContext.NotificationDevices
            .FirstOrDefaultAsync(d => d.FcmToken == token);
    }

    public async Task AddDeviceAsync(NotificationDevice device)
    {
        _dbContext.NotificationDevices.Add(device);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateDeviceAsync(NotificationDevice device)
    {
        _dbContext.NotificationDevices.Update(device);
        await _dbContext.SaveChangesAsync();
    }
}