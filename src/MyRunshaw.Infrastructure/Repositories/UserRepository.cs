using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Authentication;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByStudentIdAsync(string studentId)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.StudentId == studentId);
    }

    public async Task AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteByStudentIdAsync(string studentId)
    {
        var user = await GetByStudentIdAsync(studentId);
        if (user != null)
        {
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}