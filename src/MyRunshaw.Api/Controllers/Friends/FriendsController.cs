using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Friends;
using MyRunshaw.Contracts.Requests;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friendService;
    private readonly INameService _nameService;

    public FriendsController(IFriendService friendService, INameService nameService)
    {
        _friendService = friendService;
        _nameService = nameService;
    }

    private string CurrentStudentId =>
    User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? string.Empty;

    /// <summary>
    /// Gets the list of friends for the current user
    /// </summary>
    [HttpGet("api/friends")]
    public async Task<IActionResult> GetFriends()
    {
        // doesn't matter what status
        var friends = await _friendService.GetRequestsAsync(CurrentStudentId.ToStudentId(), "accepted");
        return Ok(friends);
    }

    /// <summary>
    /// Sends a friend request to another user
    /// </summary>
    [HttpPost("api/friend-requests")]
    public async Task<IActionResult> SendFriendRequest([FromBody] FriendRequestBody request)
    {
        try
        {
            await _friendService.SendRequestAsync(CurrentStudentId.ToStudentId(), request.receiver_id.ToStudentId());
            return Ok(new { message = "Friend request sent." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets the list of friend requests for the current user, optionally filtered by status (pending, accepted)
    /// </summary>
    [HttpGet("api/friend-requests")]
    public async Task<IActionResult> GetFriendRequests([FromQuery] string status = "pending")
    {
        var requests = await _friendService.GetRequestsAsync(CurrentStudentId.ToStudentId(), status);

        // Format to match old expectations if needed, but returning the raw objects is usually fine
        return Ok(requests.Select(r => new
        {
            id = r.Id,
            sender_id = r.SenderId.ToStudentId(),
            receiver_id = r.ReceiverId.ToStudentId(),
            status = r.Status.ToString().ToLower(),
            created_at = r.CreatedAt,
            updated_at = r.UpdatedAt
        }));
    }

    /// <summary>
    /// Respond to a friend request (accept or reject)
    /// </summary>
    [HttpPut("api/friend-requests/{request_id}")]
    public async Task<IActionResult> HandleFriendRequest(int request_id, [FromBody] FriendRequestHandleBody request)
    {
        try
        {
            await _friendService.HandleRequestAsync(CurrentStudentId.ToStudentId(), request_id, request.action);
            return Ok(new { message = $"Request {request.action}ed successfully." });
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is KeyNotFoundException)
        {
            return NotFound(new { detail = "Request not found or unauthorized." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets the name of a user by their student ID
    /// </summary>
    [HttpGet("api/name/get/{student_id}")]
    public async Task<IActionResult> GetName(string student_id)
    {
        var name = await _nameService.GetNameAsync(student_id.ToStudentId());
        return Ok(new { name });
    }

    /// <summary>
    /// Gets the names of multiple users by their student IDs
    /// </summary>
    [HttpPost("api/name/get/batch")]
    public async Task<IActionResult> BatchGetNames([FromBody] BatchGetBody request)
    {
        if (request.user_ids == null || !request.user_ids.Any())
            return BadRequest(new { error = "No student IDs provided" });

        var requestedIds = request.user_ids.Select(id => id.ToStudentId()).ToList();
        var namesDict = await _nameService.BatchGetNamesAsync(requestedIds);

        return Ok(namesDict); // e.g. { "ABC12345678": "John Smith", "DEF98765432": "Davey Demo" }
    }

    /// <summary>
    /// Removes a friendship between the current user and another user
    /// </summary>
    [HttpPost("api/block")]
    public async Task<IActionResult> BlockFriend([FromBody] BlockFriendBody request)
    {
        await _friendService.BlockFriendAsync(CurrentStudentId.ToStudentId(), request.blocked_id.ToStudentId());
        return Ok(new { message = "Friend blocked successfully." });
    }
}