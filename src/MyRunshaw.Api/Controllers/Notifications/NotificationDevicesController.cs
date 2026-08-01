using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Contracts.Requests;
using MyRunshaw.Contracts.Responses;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications/devices")]
public class NotificationDevicesController : ControllerBase
{
    private readonly INotificationDeviceService _deviceService;

    public NotificationDevicesController(INotificationDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    private string CurrentStudentId => User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? "";

    [HttpPut("{deviceId}")]
    public async Task<IActionResult> RegisterDevice(string deviceId, [FromBody] RegisterNotificationDeviceRequest request)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { detail = "deviceId and token are required." });

        await _deviceService.RegisterDeviceAsync(CurrentStudentId, deviceId, request);
        return NoContent();
    }

    [HttpGet("{deviceId}/preferences")]
    public async Task<IActionResult> GetPreferences(string deviceId)
    {
        var prefs = await _deviceService.GetPreferencesAsync(CurrentStudentId, deviceId);
        if (prefs is null) return NotFound();

        return Ok(prefs);
    }

    [HttpPut("{deviceId}/preferences")]
    public async Task<IActionResult> UpdatePreferences(string deviceId, [FromBody] UpdateNotificationDevicePreferencesRequest request)
    {
        if (request.NotificationsEnabled is null && request.BusNotificationsEnabled is null)
            return BadRequest(new { detail = "At least one preference is required." });

        try
        {
            await _deviceService.UpdatePreferencesAsync(CurrentStudentId, deviceId, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}