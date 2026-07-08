using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Friends;

public interface IFriendRepository
{
    Task<FriendRequest?> GetRequestByIdAsync(int requestId);
    Task<FriendRequest?> GetRequestBetweenUsersAsync(string studentOneId, string studentTwoId);
    Task AddRequestAsync(FriendRequest request);
    Task UpdateRequestAsync(FriendRequest request);
    Task DeleteRequestAsync(FriendRequest request);
    Task<List<FriendRequest>> GetReceivedRequestsAsync(string studentId, FriendRequestStatus? status = null);
    Task<List<string>> GetFriendIdsAsync(string studentId);
}