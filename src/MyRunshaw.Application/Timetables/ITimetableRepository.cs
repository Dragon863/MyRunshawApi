using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Timetables;

public interface ITimetableRepository
{
    Task<TimetableCache?> GetByStudentIdAsync(string studentId);
    Task AddAsync(TimetableCache timetableCache);
    Task UpdateAsync(TimetableCache timetableCache);
}