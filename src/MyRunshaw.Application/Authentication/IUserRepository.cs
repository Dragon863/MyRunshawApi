using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Authentication;

public interface IUserRepository
{
    Task<User?> GetByStudentIdAsync(string studentId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteByStudentIdAsync(string studentId);
}