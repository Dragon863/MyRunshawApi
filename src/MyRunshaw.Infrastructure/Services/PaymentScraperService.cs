using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Payments;
using MyRunshaw.Contracts.Responses;

namespace MyRunshaw.Infrastructure.Services;

public class PaymentScraperService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentScraperService> _logger;

    public PaymentScraperService(
        HttpClient httpClient,
        IUserRepository userRepository,
        IConfiguration config,
        ILogger<PaymentScraperService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5); // shouldn't ever take this long
        _userRepository = userRepository;
        _config = config;
        _logger = logger;
    }

    private async Task<string> GetPaymentIdAsync(string studentId)
    {
        var user = await _userRepository.GetByStudentIdAsync(studentId);
        if (string.IsNullOrEmpty(user?.TimetableUrl) || !user.TimetableUrl.Contains("?id="))
        {
            throw new InvalidOperationException("Please sync your timetable first to use this feature!");
        }

        return user.TimetableUrl.Split("?id=").Last();
    }

    public async Task<string> GetDeeplinkAsync(string studentId)
    {
        var paymentId = await GetPaymentIdAsync(studentId);
        var baseUrl = _config["Payments:BalanceUrl"];
        return $"{baseUrl}{paymentId}";
    }

    public async Task<string> GetBalanceAsync(string studentId)
    {
        var paymentId = await GetPaymentIdAsync(studentId);
        var url = $"{_config["Payments:BalanceUrl"]}{paymentId}";

        var html = await FetchHtmlAsync(url, studentId, "balance");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // find all h1 elements, with class "display-4"
        var balanceNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'display-4')]");

        if (balanceNode != null)
        {
            return balanceNode.InnerText.Trim();
        }

        throw new Exception("Balance information not found in the HTML content.");
    }

    public async Task<List<TransactionResponse>> GetTransactionsAsync(string studentId)
    {
        var paymentId = await GetPaymentIdAsync(studentId);
        var url = $"{_config["Payments:TransactionsUrl"]}{paymentId}";

        var html = await FetchHtmlAsync(url, studentId, "transactions");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // find all tables with the weirdly specific id
        var tableNode = doc.DocumentNode.SelectSingleNode("//table[@id='ctl00_ctl00_bodyContent_bodyContent_gvTransactions']");

        if (tableNode == null) return new List<TransactionResponse>();

        var transactions = new List<TransactionResponse>();

        // skip the first header row - it's not a transaction
        var rows = tableNode.SelectNodes(".//tr")?.Skip(1);
        if (rows == null) return transactions;

        var moneyRegex = new Regex(@"[+-]?a?£\d+\.\d+", RegexOptions.Compiled);

        foreach (var row in rows)
        {
            var cols = row.SelectNodes(".//td");
            if (cols != null && cols.Count == 4)
            {
                var dateSpan = cols[0].SelectSingleNode(".//span");
                var date = dateSpan?.InnerText.Trim() ?? "";
                var details = dateSpan?.GetAttributeValue("title", "")?.Trim() ?? "";
                var action = cols[1].InnerText.Trim();

                var amountMatch = moneyRegex.Match(cols[2].InnerHtml);
                var balanceMatch = moneyRegex.Match(cols[3].InnerHtml);

                transactions.Add(new TransactionResponse
                {
                    date = date,
                    details = details,
                    action = action,
                    amount = amountMatch.Success ? amountMatch.Value : "Err",
                    balance = balanceMatch.Success ? balanceMatch.Value : "Err"
                });
            }
        }

        return transactions;
    }

    private async Task<string> FetchHtmlAsync(string url, string studentId, string context)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("RunshawPay {Context} request timed out for user {StudentId}", context, studentId);
            throw new TimeoutException("Request to RunshawPay timed out. Please try again later.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RunshawPay {Context} request failed for user {StudentId}", context, studentId);
            throw new HttpRequestException("Failed to contact RunshawPay. Please try again later.");
        }
    }
}