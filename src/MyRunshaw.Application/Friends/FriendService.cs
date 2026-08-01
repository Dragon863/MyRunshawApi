using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Notifications;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Friends;

public class FriendService : IFriendService
{
    private readonly IFriendRepository _friendRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IUserRepository _userRepository;

    public FriendService(IFriendRepository friendRepository, IPushNotificationService pushNotificationService, IUserRepository userRepository)
    {
        _friendRepository = friendRepository;
        _pushNotificationService = pushNotificationService;
        _userRepository = userRepository;
    }

    public async Task SendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId)
        {
            throw new InvalidOperationException("You can't send a friend request to yourself!");
        }
        var existingRequest = await _friendRepository.GetRequestBetweenUsersAsync(senderId, receiverId);
        if (existingRequest != null)
        {
            if (existingRequest.Status == FriendRequestStatus.Pending)
            {
                throw new InvalidOperationException("There's already a pending friend request to this user.");
            }
            else if (existingRequest.Status == FriendRequestStatus.Accepted)
            {
                throw new InvalidOperationException("You're already friends with this user.");
            }
        }

        var newRequest = new FriendRequest
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _friendRepository.AddRequestAsync(newRequest);

        User? sender = await _userRepository.GetByStudentIdAsync(senderId);
        String senderName = sender?.Name ?? senderId;

        // Send a push notification to the receiver
        await _pushNotificationService.SendToUserAsync(receiverId.ToStudentId(), "New Friend Request", $"You have a new friend request from \"{senderName}\".", priority: 8, smallIcon: "friend", destination: "friends");
    }

    public async Task HandleRequestAsync(string studentId, int requestId, string action)
    {
        var request = await _friendRepository.GetRequestByIdAsync(requestId);

        if (request == null) throw new KeyNotFoundException("Request not found.");
        if (request.ReceiverId != studentId) throw new UnauthorizedAccessException("Not your request to handle.");
        if (request.Status != FriendRequestStatus.Pending) throw new ArgumentException("Request is no longer pending.");

        if (action.ToLower() == "accept")
        {
            request.Status = FriendRequestStatus.Accepted;
            await _friendRepository.UpdateRequestAsync(request);

            // Notify the sender that it was accepted!
            await _pushNotificationService.SendToUserAsync(request.SenderId, "Friend Request Accepted", $"{studentId.ToUpper()} accepted your request!");
        }
        else if (action.ToLower() == "decline")
        {
            await _friendRepository.DeleteRequestAsync(request);
        }
        else
        {
            throw new ArgumentException("Invalid action. Use 'accept' or 'decline'.");
        }
    }

    public async Task<List<FriendRequest>> GetRequestsAsync(string studentId, string? statusStr)
    {
        FriendRequestStatus? status = statusStr?.ToLower() == "accepted" ? FriendRequestStatus.Accepted :
                     statusStr?.ToLower() == "pending" ? FriendRequestStatus.Pending :
                     null;

        return await _friendRepository.GetReceivedRequestsAsync(studentId, status);
    }

    public async Task<List<string>> GetFriendsAsync(string studentId)
    {
        return await _friendRepository.GetFriendIdsAsync(studentId);
    }

    public async Task BlockFriendAsync(string studentId, string blockedStudentId)
    {
        // Check if they are friends
        var existingRequest = await _friendRepository.GetRequestBetweenUsersAsync(studentId.ToStudentId(), blockedStudentId.ToStudentId());
        Console.WriteLine($"Existing request between {studentId} and {blockedStudentId}: {existingRequest?.Status}");
        if (existingRequest != null && existingRequest.Status == FriendRequestStatus.Accepted)
        {
            // If they are friends, delete the friendship
            await _friendRepository.DeleteRequestAsync(existingRequest);
        }
        else
        {
            throw new InvalidOperationException("You can only block users you are friends with.");
        }
    }
}