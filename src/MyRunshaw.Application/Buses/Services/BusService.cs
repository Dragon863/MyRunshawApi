using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Friends;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Buses;

public class BusService : IBusService
{
    private readonly IBusRepository _busRepository;
    private readonly IFriendRepository _friendRepository;

    public BusService(IBusRepository busRepository, IFriendRepository friendRepository)
    {
        _busRepository = busRepository;
        _friendRepository = friendRepository;
    }

    public async Task<List<string>> GetSubscribedBusesAsync(string studentId, string forStudentId)
    {
        bool isSelf = studentId == forStudentId;
        var friendIds = await _friendRepository.GetFriendIdsAsync(studentId);
        var result = new Dictionary<string, TimetableDocument>();

        if (!isSelf && !friendIds.Contains(forStudentId))
            throw new UnauthorizedAccessException($"Unauthorised access for {forStudentId}");

        return await _busRepository.GetUserExtraBusesAsync(forStudentId);
    }

    public async Task SubscribeToBusAsync(string studentId, string busNumber)
    {
        var bus = await _busRepository.GetBusByRouteNameAsync(busNumber);
        if (bus == null) throw new ArgumentException($"Bus {busNumber} not found.");

        await _busRepository.AddSubscriptionAsync(new BusSubscription
        {
            StudentId = studentId,
            BusId = bus.BusId,
        });
    }

    public async Task UnsubscribeFromBusAsync(string studentId, string busNumber)
    {
        var bus = await _busRepository.GetBusByRouteNameAsync(busNumber);
        if (bus != null)
        {
            // bus exists; remove it
            await _busRepository.RemoveSubscriptionAsync(studentId, bus.BusId);
        }
    }

    public Task<List<Bus>> GetAllBusesAsync()
    {
        return _busRepository.GetAllBusesAsync();
    }

    public async Task<List<BusStop>> GetStopsByBusIdAsync(string busId)
    {
        return await _busRepository.GetStopsByBusIdAsync(busId);
    }

    public async Task<string?> GetBusRouteDescriptionAsync(string busId)
    {
        return await _busRepository.GetBusRouteDescriptionAsync(busId);
    }
}