using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Buses;
using MyRunshaw.Domain.Entities;
using MyRunshaw.Infrastructure.Database;

namespace MyRunshaw.Infrastructure.Repositories;

public class BusRepository : IBusRepository
{
    private readonly AppDbContext _dbContext;

    public BusRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Bus?> GetBusByRouteNameAsync(string routeName)
    {
        return await _dbContext.Buses.FirstOrDefaultAsync(b => b.BusId == routeName);
    }

    public async Task<List<string>> GetUserExtraBusesAsync(string studentId)
    {
        return await _dbContext.BusSubscriptions
            .Where(bs => bs.StudentId == studentId)
            .Select(bs => bs.Bus.BusId)
            .ToListAsync();
    }

    public async Task AddSubscriptionAsync(BusSubscription subscription)
    {
        // don't add if it already exists
        var exists = await _dbContext.BusSubscriptions.AnyAsync(bs =>
            bs.StudentId == subscription.StudentId && bs.BusId == subscription.BusId);

        if (!exists)
        {
            _dbContext.BusSubscriptions.Add(subscription);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveSubscriptionAsync(string studentId, string busId)
    {
        var subscription = await _dbContext.BusSubscriptions
            .FirstOrDefaultAsync(bs => bs.StudentId == studentId && bs.BusId == busId);

        if (subscription != null)
        {
            _dbContext.BusSubscriptions.Remove(subscription);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddBusAsync(Bus bus)
    {
        var exists = await _dbContext.Buses.AnyAsync(b => b.BusId == bus.BusId);

        if (!exists)
        {
            _dbContext.Buses.Add(bus);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetSubscribersForBusAsync(string busId)
    {
        return await _dbContext.BusSubscriptions
            .Where(bs => bs.Bus.BusId == busId)
            .Select(bs => bs.StudentId) // we only want the list of strings
            .ToListAsync();
    }

    public async Task<List<Bus>> GetAllBusesAsync()
    {
        return await _dbContext.Buses.ToListAsync();
    }

    public async Task UpdateBusAsync(Bus bus)
    {
        _dbContext.Buses.Update(bus);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ResetAllBusesToZeroAsync()
    {
        await _dbContext.Buses
            .Where(b => b.CurrentBay != "0")
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.CurrentBay, "0"));
    }

    public async Task ReplaceBusStopsAsync(string busId, List<BusStop> newStops)
    {
        // delete old stops...
        var oldStops = await _dbContext.BusStops.Where(s => s.BusId == busId).ToListAsync();
        _dbContext.BusStops.RemoveRange(oldStops);

        // ...and add new stops
        foreach (var stop in newStops)
        {
            stop.BusId = busId;
        }

        _dbContext.BusStops.AddRange(newStops);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<BusStop>> GetStopsByBusIdAsync(string busId)
    {
        return await _dbContext.BusStops
            .Where(s => s.BusId == busId)
            .ToListAsync();
    }
    public async Task<string?> GetBusRouteDescriptionAsync(string busId)
    {
        var bus = await _dbContext.Buses.FirstOrDefaultAsync(b => b.BusId == busId);
        return bus?.RouteDescription;
    }
}

