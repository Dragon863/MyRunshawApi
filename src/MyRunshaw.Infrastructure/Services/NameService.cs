using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MyRunshaw.Application.Friends;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Services;

public class NameService : INameService
{
    private readonly AppDbContext _dbContext;
    private readonly IDistributedCache _cache;

    public NameService(AppDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<string> GetNameAsync(string studentId)
    {
        // checks cache, falls back to DB, caches if not found. Returns the name of the given student.
        var cacheKey = $"user_name:{studentId}";
        var cachedName = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedName))
        {
            return cachedName;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.StudentId == studentId);
        var name = user?.Name ?? "Unknown User";

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        };
        await _cache.SetStringAsync(cacheKey, name, cacheOptions);
        return name;
    }

    public async Task<Dictionary<string, string>> BatchGetNamesAsync(List<string> studentIds)
    {
        var result = new Dictionary<string, string>();
        var missingIds = new List<string>();

        var cacheTasks = studentIds.Select(async id =>
        {
            var name = await _cache.GetStringAsync($"user_name:{id}");
            return new { Id = id, Name = name };
        });

        var cacheResults = await Task.WhenAll(cacheTasks);

        foreach (var item in cacheResults)
        {
            if (!string.IsNullOrEmpty(item.Name))
            {
                result[item.Id] = item.Name;
            }
            else
            {
                missingIds.Add(item.Id);
            }
        }

        foreach (var item in cacheResults)
        {
            if (!string.IsNullOrEmpty(item.Name))
            {
                result[item.Id] = item.Name;
            }
            else
            {
                missingIds.Add(item.Id);
            }
        }

        // DB lookup + add to cache for IDs missed in cache
        if (missingIds.Any())
        {
            var dbUsers = await _dbContext.Users
                .Where(u => missingIds.Contains(u.StudentId))
                .ToDictionaryAsync(u => u.StudentId, u => u.Name);

            foreach (var id in missingIds)
            {
                var name = dbUsers.GetValueOrDefault(id, "Unknown User");
                result[id] = name;

                // cache the result for future lookups
                _ = _cache.SetStringAsync(
                    $"user_name:{id}",
                    name,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) }
                );
            }
        }

        return result;
    }
}