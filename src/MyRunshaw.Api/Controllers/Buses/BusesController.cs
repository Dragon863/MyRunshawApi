using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Buses;
using MyRunshaw.Contracts.Requests;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Authorize]
public class BusesController : ControllerBase
{
    private readonly IBusService _busService;

    public BusesController(IBusService busService)
    {
        _busService = busService;
    }

    private string CurrentStudentId =>
    User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? string.Empty;

    /// <summary>
    /// Gets the list of buses subscribed to for notifications
    /// </summary>
    [HttpGet("api/extra_buses/get")] // legacy compatibility with old API :(
    public async Task<IActionResult> GetExtraBuses()
    {
        var buses = await _busService.GetSubscribedBusesAsync(CurrentStudentId, CurrentStudentId);
        // convert to JSON array of strings e.g. [{"bus": "801"}, {"bus": "802"}] for legacy compatibility
        var jsonResponse = buses.Select(b => new { bus = b });
        return Ok(jsonResponse);
    }

    /// <summary>
    /// Subscribes to a bus for notifications
    /// </summary>
    [HttpPost("api/extra_buses/add")]
    public async Task<IActionResult> AddExtraBus([FromBody] ExtraBusRequest request)
    {
        try
        {
            await _busService.SubscribeToBusAsync(CurrentStudentId, request.bus_number);
            return Ok(new { message = "Subscribed successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Unsubscribes from a bus for notifications
    /// </summary>
    [HttpPost("api/extra_buses/remove")]
    public async Task<IActionResult> RemoveExtraBus([FromBody] ExtraBusRequest request)
    {
        await _busService.UnsubscribeFromBusAsync(CurrentStudentId, request.bus_number);
        return Ok(new { message = "Unsubscribed successfully" });
    }

    /// <summary>
    /// Gets all the buses and their current bay and arrival time. Used for the bus arrivals page.
    /// </summary>
    [HttpGet("api/bus")]
    public async Task<IActionResult> GetAllBuses()
    {
        List<Bus> Buses = await _busService.GetAllBusesAsync();
        IEnumerable<dynamic> JsonResult = Buses.Select(b => new
        {
            bus_id = b.BusId,
            bus_bay = b.CurrentBay,
            arrival_time = b.UpdatedAt.ToString("o"), // ISO8601
            route_map = b.MapsRouteUrl,
        });
        return Ok(JsonResult.ToList().OrderBy(a => a.bus_id));
    }

    /// <summary>
    /// Gets the buses a friend is subscribed to for notifications as a string e.g. 803, 105, 112
    /// </summary>
    [HttpGet("api/bus/for")]
    public async Task<IActionResult> GetBusesForFriend([FromQuery] string user_id)
    {
        try
        {
            var buses = await _busService.GetSubscribedBusesAsync(CurrentStudentId, user_id);
            if (buses == null || !buses.Any())
                return Ok("No buses added");
            else
                return Ok(string.Join(", ", buses));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Gets every stop on a bus route
    /// </summary>
    [HttpGet("api/bus/stops")]
    public async Task<IActionResult> GetBusStops([FromQuery] string bus_id)
    {
        // normalize the casing; shouldn't happen but handle just in case
        var normalizedBusId = bus_id.Trim().ToUpperInvariant();

        var stops = await _busService.GetStopsByBusIdAsync(normalizedBusId);

        if (stops == null || !stops.Any())
        {
            return NotFound(new { message = $"No stops found for bus {normalizedBusId}" });
        }

        var response = new
        {
            stops = stops.Select(s => new
            {
                name = s.Name,
                latitude = s.Latitude,
                longitude = s.Longitude
            }),
            description = await _busService.GetBusRouteDescriptionAsync(normalizedBusId)
        };

        return Ok(response);
    }
}