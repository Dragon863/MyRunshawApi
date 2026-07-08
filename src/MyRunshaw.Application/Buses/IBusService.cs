using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Buses;

public interface IBusService
{
    Task<List<string>> GetSubscribedBusesAsync(string currentStudentId, string forStudentId);
    Task SubscribeToBusAsync(string studentId, string busNumber);
    Task UnsubscribeFromBusAsync(string studentId, string busNumber);
    Task<List<Bus>> GetAllBusesAsync();
    Task<List<BusStop>> GetStopsByBusIdAsync(string busId);
    Task<string?> GetBusRouteDescriptionAsync(string busId);
}