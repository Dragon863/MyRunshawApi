using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Timetables;
using MyRunshaw.Contracts.Requests;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Authorize]
public class TimetablesController : ControllerBase
{
    private readonly ITimetableService _timetableService;

    public TimetablesController(ITimetableService timetableService)
    {
        _timetableService = timetableService;
    }

    private string CurrentStudentId => User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? "";

    /// <summary>
    /// Upload timetable (deprecated and nonfunctional, don't use)
    /// </summary>
    [HttpPost("api/timetable")]
    [Obsolete("Preserved for legacy support. Use /api/timetable/associate instead.")]
    public IActionResult AddTimetableMock()
    {
        // Deprecated route, just returns 201 so the old app doesn't break. yay!
        return StatusCode(201, new { message = "Timetable uploaded successfully" });
    }

    /// <summary>
    /// Gets the timetable for the current user as JSON
    /// </summary>
    [HttpGet("api/timetable")]
    public async Task<IActionResult> GetTimetable([FromQuery] string? user_id)
    {
        var targetId = string.IsNullOrEmpty(user_id) ? CurrentStudentId : user_id.ToStudentId();

        try
        {
            var doc = await _timetableService.GetTimetableAsync(CurrentStudentId, targetId);
            return Ok(new { timetable = doc });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "Unauthorised access" });
        }
    }

    /// <summary>
    /// Gets multiple timetables at once
    /// </summary>
    [HttpPost("api/timetable/batch_get")]
    public async Task<IActionResult> BatchGetTimetables([FromBody] BatchGetBody request)
    {
        if (request.user_ids == null || !request.user_ids.Any())
            return BadRequest(new { error = "No user IDs provided" });

        var requestedIds = request.user_ids.Select(id => id.ToStudentId()).ToList();

        try
        {
            var dict = await _timetableService.BatchGetTimetablesAsync(CurrentStudentId, requestedIds);

            // reformatted to match old API response
            var responseObj = dict.ToDictionary(
                kvp => kvp.Key,
                kvp => new { data = kvp.Value.Data }
            );

            return Ok(responseObj);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "Unauthorised access" });
        }
    }

    /// <summary>
    /// After being associated, this enables timetable and payment related features
    /// </summary>
    [HttpPost("api/timetable/associate")]
    public async Task<IActionResult> AssociateUrl([FromBody] TimetableAssociationBody body)
    {
        try
        {
            await _timetableService.AssociateUrlAsync(CurrentStudentId, body.url);
            return StatusCode(201, new { message = "Timetable URL associated successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Failed to associate timetable URL" });
        }
    }
}