using System.ComponentModel.DataAnnotations;

namespace MyRunshaw.Domain.Entities;

public class NotificationDevice
{
    public int Id { get; set; }

    [MaxLength(11)]
    public string StudentId { get; set; } = string.Empty;
    public User Student { get; set; } = null!;

    [MaxLength(200)]
    public string DeviceId { get; set; } = string.Empty;

    [MaxLength(4096)]
    public string? FcmToken { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(32)]
    public string? Platform { get; set; }

    [MaxLength(64)]
    public string? AppVersion { get; set; }

    public bool NotificationsEnabled { get; set; } = true;
    public bool BusNotificationsEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
