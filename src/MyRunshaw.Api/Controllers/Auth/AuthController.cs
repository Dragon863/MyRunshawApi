using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Common;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IUserRepository userRepository, IConfiguration configuration)
    {
        _authService = authService;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public class LoginRequest { public string ProviderToken { get; set; } = string.Empty; }

    /// <summary>
    /// Login via Entra, providing a JWT
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var token = await _authService.LoginWithEntraAsync(request.ProviderToken);
            return Ok(new { Token = token });
        }
        catch (UnauthorizedAccessException ex)
        {
            // bad token
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Invalid token." });
        }
    }

    /// <summary>
    /// Login via a secret bypass for App Store review ONLY.
    /// </summary>
    [HttpPost("demo")]
    [AllowAnonymous] // anyone can hit this, but it REQUIRES the secret
    public async Task<IActionResult> DemoLogin([FromBody] DemoRequest request)
    {
        var expectedSecret = _configuration["Auth:BypassSecret"];

        if (string.IsNullOrEmpty(expectedSecret) || request.Secret != expectedSecret)
        {
            return Unauthorized(new { message = "Invalid bypass secret." });
        }

        var demoStudentId = _configuration["Auth:DemoStudentId"];
        var demoTimetableUrl = _configuration["Auth:DemoTimetableUrl"];

        if (string.IsNullOrEmpty(demoStudentId) || string.IsNullOrEmpty(demoTimetableUrl))
        {
            return StatusCode(500, new { message = "Demo student ID or timetable URL not configured." });
        }

        var user = await _userRepository.GetByStudentIdAsync(demoStudentId);

        if (user == null)
        {
            user = new User
            {
                StudentId = demoStudentId,
                Name = "App Reviewer",
                TimetableUrl = demoTimetableUrl,
            };
            await _userRepository.AddAsync(user);
        }

        var token = ((AuthService)_authService).GenerateCustomJwt(user);
        return Ok(new { Token = token });
    }

    /// <summary>
    /// Close the current user's account and delete all associated data.
    /// </summary>
    [HttpPost("close_account")]
    [Authorize]
    public async Task<IActionResult> CloseAccount()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId();
        if (string.IsNullOrEmpty(studentId))
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        try
        {
            await _userRepository.DeleteByStudentIdAsync(studentId);
            return Ok(new { message = "Account closed successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to close account: {ex.Message}" });
        }
    }

#if DEBUG
    // ONLY EXISTS IN LOCAL DEVELOPMENT! easy bypass for entra login for any student ID
    [HttpPost("dev-login/{studentId}")]
    public async Task<IActionResult> DevLogin(string studentId)
    {
        var user = await _userRepository.GetByStudentIdAsync(studentId);
        if (user == null)
        {
            user = new User { StudentId = studentId };
            await _userRepository.AddAsync(user);
        }

        var token = ((AuthService)_authService).GenerateCustomJwt(user);
        return Ok(new { Token = token });
    }
#endif
}