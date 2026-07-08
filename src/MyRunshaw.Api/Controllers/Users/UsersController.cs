using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Users;
using MyRunshaw.Contracts.Requests;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IProfileService _profileService;

    public UsersController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    private string CurrentStudentId => User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? "";

    /// <summary>
    /// Upload a new profile picture for the current user with a file as multipart/form-data
    /// </summary>
    [HttpPost("me/profile-pic")]
    public async Task<IActionResult> UploadProfilePic(IFormFile file)
    {
        try
        {
            var url = await _profileService.UploadProfilePictureAsync(CurrentStudentId, file);
            return Ok(new { message = "Profile picture updated successfully", url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Clears the current user's profile picture
    /// </summary>
    [HttpDelete("me/profile-pic")]
    public async Task<IActionResult> ClearProfilePic()
    {
        try
        {
            await _profileService.DeleteProfilePictureAsync(CurrentStudentId);
            return Ok(new { message = "Profile picture cleared successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Updates the user's own name
    /// </summary>
    [HttpPost("me/name")]
    public async Task<IActionResult> UpdateName([FromBody] UpdateNameRequest request)
    {
        try
        {
            await _profileService.UpdateNameAsync(CurrentStudentId, request.new_name);
            return Ok(new { message = "Name updated successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { detail = ex.Message });
        }
    }
}