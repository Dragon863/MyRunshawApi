using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Timetables;
using MyRunshaw.Infrastructure.Database;
using Quartz;

namespace MyRunshaw.Workers.Jobs;

[DisallowConcurrentExecution]
public class TimetableDailySyncJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TimetableDailySyncJob> _logger;

    public TimetableDailySyncJob(IServiceProvider serviceProvider, ILogger<TimetableDailySyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting daily timetable sync at {Time}", DateTimeOffset.Now);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<ITimetableSyncService>();

        // only users with a non-empty timetable URL will be synced
        var usersToSync = await dbContext.Users
            .Where(u => !string.IsNullOrEmpty(u.TimetableUrl))
            .Select(u => new { u.StudentId, u.TimetableUrl })
            .ToListAsync(context.CancellationToken);

        _logger.LogInformation("Found {Count} timetables to sync.", usersToSync.Count);

        // sync sequentially
        foreach (var user in usersToSync)
        {
            try
            {
                await syncService.SyncTimetableAsync(user.StudentId, user.TimetableUrl!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync timetable for {StudentId}", user.StudentId);
            }
        }

        _logger.LogInformation("Daily timetable sync completed successfully.");
    }
}