using System.ComponentModel.DataAnnotations;

namespace MyRunshaw.Domain.Entities;

public class User
{
    [Key] // let EF Core know this is the Primary Key
    [MaxLength(11)] // ABC12345678
    public string StudentId { get; set; } = string.Empty;

    public string Name { get; set; } = "Unknown User";

    public DateTime SignupDate { get; set; } = DateTime.UtcNow;

    // Merged from profile_pics in old API
    public int ProfilePicVersion { get; set; } = 1;

    // Merged from timetable_associations
    public string? TimetableUrl { get; set; }

    // Navigation Properties (Helps EF Core understand relationships)
    public ICollection<BusSubscription> BusSubscriptions { get; set; } = new List<BusSubscription>();
    public ICollection<FriendRequest> SentFriendRequests { get; set; } = new List<FriendRequest>();
    public ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = new List<FriendRequest>();
}