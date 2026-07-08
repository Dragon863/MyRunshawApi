namespace MyRunshaw.Contracts.Requests;

public class FriendRequestHandleBody
{
    public string action { get; set; } = string.Empty; // e.g., "accept" or "decline"
}