using Microsoft.EntityFrameworkCore;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Repositories;

public class NoticeRepository : IInAppNoticeRepository
{
    private readonly AppDbContext _dbContext;

    public NoticeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<InAppNotice>> GetNoticesAsync()
    {
        return await _dbContext.InAppNotices.ToListAsync();
    }
}