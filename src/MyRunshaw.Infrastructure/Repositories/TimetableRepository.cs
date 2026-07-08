using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Timetables;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Repositories;

public class TimetableRepository : ITimetableRepository
{
    private readonly AppDbContext _dbContext;

    public TimetableRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TimetableCache?> GetByStudentIdAsync(string studentId)
    {
        return await _dbContext.Timetables.FirstOrDefaultAsync(t => t.StudentId == studentId);
    }

    public async Task AddAsync(TimetableCache timetableCache)
    {
        _dbContext.Timetables.Add(timetableCache);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(TimetableCache timetableCache)
    {
        _dbContext.Timetables.Update(timetableCache);
        await _dbContext.SaveChangesAsync();

    }
}