using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;

    public SyncController(ISyncService syncService)
    {
        _syncService = syncService;
    }

    private string CurrentStudentId => User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? "";

    /// <summary>
    /// Called on app startup, gathers all data needed for the home page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSyncData()
    {
        var syncData = await _syncService.GetSyncPayloadAsync(CurrentStudentId);
        return Ok(syncData);
    }
}