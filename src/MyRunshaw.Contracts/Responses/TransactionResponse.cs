namespace MyRunshaw.Contracts.Responses;

public class TransactionResponse
{
    public string date { get; set; } = string.Empty;
    public string details { get; set; } = string.Empty;
    public string action { get; set; } = string.Empty;
    public string amount { get; set; } = string.Empty;
    public string balance { get; set; } = string.Empty;
}