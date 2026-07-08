using MyRunshaw.Domain.Entities;

public interface ITimetableService
{
    Task AssociateUrlAsync(string studentId, string url);
    Task<TimetableDocument> GetTimetableAsync(string requesterId, string targetStudentId);
    Task<Dictionary<string, TimetableDocument>> BatchGetTimetablesAsync(string requesterId, List<string> targetStudentIds);
}