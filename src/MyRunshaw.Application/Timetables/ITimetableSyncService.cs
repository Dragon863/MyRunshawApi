namespace MyRunshaw.Application.Timetables;

public interface ITimetableSyncService
{
    Task SyncTimetableAsync(string studentId, string url);
}