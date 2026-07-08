using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Contracts.Responses;

public class SyncResponse
{
    public CurrentUserSync Me { get; set; } = new();
    public List<FriendSync> Friends { get; set; } = new();
    public List<FriendRequestSync> PendingRequests { get; set; } = new();
    public List<KeyValuePair<string, string>> SubscribedBusStatuses { get; set; } = new();

    public Dictionary<string, TimetableDocument> Timetables { get; set; } = new();
}


public class CurrentUserSync
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProfilePicVersion { get; set; }
    public bool HasTimetableLinked { get; set; }
}

public class FriendSync
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProfilePicVersion { get; set; }
}

public class FriendRequestSync
{
    public int RequestId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}