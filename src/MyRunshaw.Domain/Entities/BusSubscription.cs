namespace MyRunshaw.Domain.Entities;

public class BusSubscription
{
    public string StudentId { get; set; } = string.Empty;
    public User Student { get; set; } = null!;

    public string BusId { get; set; } = string.Empty;
    public Bus Bus { get; set; } = null!;

    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
}