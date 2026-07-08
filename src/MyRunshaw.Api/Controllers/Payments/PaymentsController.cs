using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Payments;

namespace MyRunshaw.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private string CurrentStudentId => User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToStudentId() ?? "";

    /// <summary>
    /// Fetches RunshawPay balance
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var balance = await _paymentService.GetBalanceAsync(CurrentStudentId);
            return Ok(new { balance });
        }
        catch (InvalidOperationException ex) // user is missing a timetable
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (TimeoutException ex) // 408 Timeout (happens ~5% of the time sadly)
        {
            return StatusCode(408, new { detail = ex.Message });
        }
        catch (HttpRequestException ex) // 502 Bad Gateway (college broke something)
        {
            return StatusCode(502, new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets the list of transactions for the current user in JSON
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        try
        {
            var transactions = await _paymentService.GetTransactionsAsync(CurrentStudentId);
            return Ok(transactions);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(408, new { detail = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Gets the RunshawPay URL for the current user, which can be opened in a browser
    /// </summary>
    [HttpGet("deeplink")]
    public async Task<IActionResult> GetDeeplink()
    {
        try
        {
            var deeplink = await _paymentService.GetDeeplinkAsync(CurrentStudentId);
            return Ok(new { deeplink });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500, new { detail = "Timetable not synced" });
        }
    }
}