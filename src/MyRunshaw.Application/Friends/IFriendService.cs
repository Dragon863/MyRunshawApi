using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Friends;

public interface IFriendService
{
    Task SendRequestAsync(string senderId, string receiverId);
    Task HandleRequestAsync(string studentId, int requestId, string action);
    Task<List<FriendRequest>> GetRequestsAsync(string studentId, string? status);
    Task<List<string>> GetFriendsAsync(string studentId);
    Task BlockFriendAsync(string studentId, string blockedStudentId);
}