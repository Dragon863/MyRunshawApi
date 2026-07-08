using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Buses;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Friends;
using MyRunshaw.Contracts.Responses;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Sync;

public class SyncService : ISyncService
{
    private readonly IUserRepository _userRepository;
    private readonly IFriendService _friendService;
    private readonly INameService _nameService;
    private readonly IBusService _busService;
    private readonly ITimetableService _timetableService;

    public SyncService(
        IUserRepository userRepository,
        IFriendService friendService,
        INameService nameService,
        IBusService busService,
        ITimetableService timetableService)
    {
        _userRepository = userRepository;
        _friendService = friendService;
        _nameService = nameService;
        _busService = busService;
        _timetableService = timetableService;
    }

    public async Task<SyncResponse> GetSyncPayloadAsync(string studentId)
    {
        // can't use Task.WhenAll here the database services are not thread-safe - we need to await them sequentially
        var user = await _userRepository.GetByStudentIdAsync(studentId) ?? new User { StudentId = studentId };
        var friendIds = await _friendService.GetFriendsAsync(studentId);
        var pendingRequests = await _friendService.GetRequestsAsync(studentId, "pending");
        var subscribedBuses = await _busService.GetSubscribedBusesAsync(studentId, studentId);
        var busStatus = await _busService.GetAllBusesAsync();

        // process friends' names and timetables
        var allIdsForNames = friendIds.ToList();
        allIdsForNames.AddRange(pendingRequests.Select(r => r.SenderId));

        var allIdsForTimetables = friendIds.ToList();
        allIdsForTimetables.Add(studentId);

        // get friends' names and timetables
        var namesDictionary = await _nameService.BatchGetNamesAsync(allIdsForNames);
        var timetablesDictionary = await _timetableService.BatchGetTimetablesAsync(studentId, allIdsForTimetables);

        // process buses into KV pair
        var busStatusDictionary = busStatus.ToDictionary(b => b.BusId, b => b);

        var subscribedBusStatuses = subscribedBuses
            .Where(b => busStatusDictionary.ContainsKey(b)) // just in case!
            .Select(b => new KeyValuePair<string, string>(b, busStatusDictionary[b].CurrentBay))
            .ToList();

        var friendsList = new List<FriendSync>();

        foreach (var fid in friendIds)
        {
            var friendUser = await _userRepository.GetByStudentIdAsync(fid);

            friendsList.Add(new FriendSync
            {
                StudentId = fid,
                Name = namesDictionary.GetValueOrDefault(fid, "Unknown User"),
                ProfilePicVersion = friendUser?.ProfilePicVersion ?? 1
            });
        }


        var response = new SyncResponse
        {
            Me = new CurrentUserSync
            {
                StudentId = user.StudentId,
                Name = user.Name,
                ProfilePicVersion = user.ProfilePicVersion,
                HasTimetableLinked = !string.IsNullOrEmpty(user.TimetableUrl),
            },
            SubscribedBusStatuses = subscribedBusStatuses,
            Timetables = timetablesDictionary,
            Friends = friendsList,
            PendingRequests = pendingRequests.Select(req => new FriendRequestSync
            {
                RequestId = req.Id,
                SenderId = req.SenderId,
                SenderName = namesDictionary.GetValueOrDefault(req.SenderId, "Unknown User"),
                CreatedAt = req.CreatedAt,
            }).ToList()
        };

        return response;
    }
}