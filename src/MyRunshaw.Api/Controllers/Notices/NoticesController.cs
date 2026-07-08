using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Friends;
using MyRunshaw.Application.Notices;
using MyRunshaw.Contracts.Requests;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Authorize]
public class NoticesController : ControllerBase
{
    private readonly INoticeService _noticeService;

    public NoticesController(INoticeService noticeService)
    {
        _noticeService = noticeService;
    }

    private string CurrentStudentId =>
    User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? string.Empty;

    /// <summary>
    /// Gets all in-app notices
    /// </summary>
    [HttpGet("api/notices")]
    public async Task<IActionResult> GetNotices()
    {
        var notices = await _noticeService.GetNoticesAsync();
        return Ok(notices);
    }
}