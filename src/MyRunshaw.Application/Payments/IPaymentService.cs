using MyRunshaw.Contracts.Responses;

namespace MyRunshaw.Application.Payments;

public interface IPaymentService
{
    Task<string> GetBalanceAsync(string studentId);
    Task<List<TransactionResponse>> GetTransactionsAsync(string studentId);
    Task<string> GetDeeplinkAsync(string studentId);
}