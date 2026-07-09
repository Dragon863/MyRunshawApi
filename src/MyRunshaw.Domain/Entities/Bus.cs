using System.ComponentModel.DataAnnotations;

namespace MyRunshaw.Domain.Entities;

public class Bus
{
    [Key]
    public string BusId { get; set; } = string.Empty;

    public string RouteDescription { get; set; } = string.Empty;

    public string? MapsRouteUrl { get; set; } // Stores the Google Maps route URL for the bus, if available.

    public string CurrentBay { get; set; } = "0"; // defaults to 0; not arrived
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BusSubscription> Subscriptions { get; set; } = new List<BusSubscription>();
    public ICollection<BusStop> BusStops { get; set; } = new List<BusStop>();
}