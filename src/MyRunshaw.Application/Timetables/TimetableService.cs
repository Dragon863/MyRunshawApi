using MyRunshaw.Application.Friends;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Application.Authentication;

namespace MyRunshaw.Application.Timetables;

public class TimetableService : ITimetableService
{
    private readonly ITimetableSyncService _syncService;
    private readonly IFriendRepository _friendRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITimetableRepository _timetableRepository; // Changed!

    public TimetableService(
        ITimetableSyncService syncService,
        IFriendRepository friendRepository,
        IUserRepository userRepository,
        ITimetableRepository timetableRepository) // Changed!
    {
        _syncService = syncService;
        _friendRepository = friendRepository;
        _userRepository = userRepository;
        _timetableRepository = timetableRepository;
    }

    public async Task AssociateUrlAsync(string studentId, string url)
    {
        var user = await _userRepository.GetByStudentIdAsync(studentId);
        if (user != null)
        {
            user.TimetableUrl = url;
            await _userRepository.UpdateAsync(user);
        }

        await _syncService.SyncTimetableAsync(studentId, url);
    }

    public async Task<TimetableDocument> GetTimetableAsync(string requesterId, string targetStudentId)
    {
        if (requesterId != targetStudentId)
        {
            var friendIds = await _friendRepository.GetFriendIdsAsync(requesterId);
            if (!friendIds.Contains(targetStudentId))
                throw new UnauthorizedAccessException("You are not friends with this user.");
        }

        var cache = await _timetableRepository.GetByStudentIdAsync(targetStudentId);
        return cache?.Data ?? new TimetableDocument { Data = new List<TimetableEvent>() };
    }

    public async Task<Dictionary<string, TimetableDocument>> BatchGetTimetablesAsync(string requesterId, List<string> targetStudentIds)
    {
        var friendIds = await _friendRepository.GetFriendIdsAsync(requesterId);
        var result = new Dictionary<string, TimetableDocument>();

        foreach (var targetId in targetStudentIds)
        {
            if (requesterId != targetId && !friendIds.Contains(targetId))
                throw new UnauthorizedAccessException($"Unauthorised access for {targetId}");

            var cache = await _timetableRepository.GetByStudentIdAsync(targetId);
            result[targetId] = cache?.Data ?? new TimetableDocument { Data = new List<TimetableEvent>() };
        }

        return result;
    }
}