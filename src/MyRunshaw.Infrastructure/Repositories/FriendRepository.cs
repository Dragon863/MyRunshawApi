using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Friends;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Repositories;

public class FriendRepository : IFriendRepository
{
    private readonly AppDbContext _dbContext;

    public FriendRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FriendRequest?> GetRequestByIdAsync(int requestId)
    {
        return await _dbContext.FriendRequests.FirstOrDefaultAsync(fr => fr.Id == requestId);
    }

    public async Task<FriendRequest?> GetRequestBetweenUsersAsync(string studentOneId, string studentTwoId)
    {
        var normalizedStudentOneId = studentOneId.ToStudentId();
        var normalizedStudentTwoId = studentTwoId.ToStudentId();

        return await _dbContext.FriendRequests.FirstOrDefaultAsync(fr =>
            (fr.SenderId.ToLower() == normalizedStudentOneId && fr.ReceiverId.ToLower() == normalizedStudentTwoId) ||
            (fr.SenderId.ToLower() == normalizedStudentTwoId && fr.ReceiverId.ToLower() == normalizedStudentOneId));
    }

    public async Task AddRequestAsync(FriendRequest request)
    {
        request.SenderId = request.SenderId.ToStudentId();
        request.ReceiverId = request.ReceiverId.ToStudentId();
        _dbContext.FriendRequests.Add(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateRequestAsync(FriendRequest request)
    {
        request.SenderId = request.SenderId.ToStudentId();
        request.ReceiverId = request.ReceiverId.ToStudentId();
        _dbContext.FriendRequests.Update(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteRequestAsync(FriendRequest request)
    {
        _dbContext.FriendRequests.Remove(request);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<FriendRequest>> GetReceivedRequestsAsync(string studentId, FriendRequestStatus? status = null)
    {
        var query = _dbContext.FriendRequests.Where(fr => fr.ReceiverId == studentId);

        if (status.HasValue)
        {
            query = query.Where(fr => fr.Status == status.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<List<string>> GetFriendIdsAsync(string studentId)
    {
        var friendsAsStudentOne = await _dbContext.FriendRequests
            .Where(fr => fr.SenderId == studentId && fr.Status == FriendRequestStatus.Accepted)
            .Select(fr => fr.ReceiverId)
            .ToListAsync();

        var friendsAsStudentTwo = await _dbContext.FriendRequests
            .Where(fr => fr.ReceiverId == studentId && fr.Status == FriendRequestStatus.Accepted)
            .Select(fr => fr.SenderId)
            .ToListAsync();

        return friendsAsStudentOne.Concat(friendsAsStudentTwo).ToList();
    }
}
