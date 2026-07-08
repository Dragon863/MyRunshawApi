namespace MyRunshaw.Domain.Entities;

public enum FriendRequestStatus
{
    Pending,
    Accepted,
    // Rejected // Not used; declining a friend request simply deletes it
}

public class FriendRequest
{
    public int Id { get; set; } // Auto-increment

    public string SenderId { get; set; } = string.Empty;
    public User Sender { get; set; } = null!;

    public string ReceiverId { get; set; } = string.Empty;
    public User Receiver { get; set; } = null!;

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}