namespace MyRunshaw.Domain.Entities;

public class BlockedUser
{
    public string BlockerId { get; set; } = string.Empty;
    public User Blocker { get; set; } = null!;

    public string BlockedId { get; set; } = string.Empty;
    public User Blocked { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}