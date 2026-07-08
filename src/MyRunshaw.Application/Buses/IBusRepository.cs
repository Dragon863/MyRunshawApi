using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Buses;

public interface IBusRepository
{
    Task<Bus?> GetBusByRouteNameAsync(string routeName);
    Task<List<string>> GetUserExtraBusesAsync(string studentId);
    Task AddSubscriptionAsync(BusSubscription subscription);
    Task RemoveSubscriptionAsync(string studentId, string busId);
    Task AddBusAsync(Bus bus);
    Task<List<string>> GetSubscribersForBusAsync(string busId);
    Task<List<Bus>> GetAllBusesAsync();
    Task UpdateBusAsync(Bus bus);
    Task ResetAllBusesToZeroAsync();
    Task ReplaceBusStopsAsync(string busId, List<BusStop> newStops);
    Task<List<BusStop>> GetStopsByBusIdAsync(string busId);
    Task<string?> GetBusRouteDescriptionAsync(string busId);

}