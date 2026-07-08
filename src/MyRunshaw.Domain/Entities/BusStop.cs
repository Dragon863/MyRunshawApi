
using MyRunshaw.Domain.Entities;

public class BusStop
{
    public int Id { get; set; }

    public string BusId { get; set; } = string.Empty;
    public Bus Bus { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
}