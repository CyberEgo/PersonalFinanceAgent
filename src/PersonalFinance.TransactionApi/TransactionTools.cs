using System.ComponentModel;
using ModelContextProtocol.Server;
using PersonalFinance.TransactionApi.Services;

namespace PersonalFinance.TransactionApi;

[McpServerToolType]
public sealed class TransactionTools(ITransactionService transactionService)
{
    [McpServerTool, Description("Get the last transactions for a bank account, ordered by date. Default is 10 transactions.")]
    public async Task<object> GetLastTransactions(string accountId, int count = 10)
        => await transactionService.GetLastTransactionsAsync(accountId, count);

    [McpServerTool, Description("Search transactions by recipient/payee name for a specific account")]
    public async Task<object> GetTransactionsByRecipientName(string accountId, string recipientName)
        => await transactionService.GetTransactionsByRecipientAsync(accountId, recipientName);

    [McpServerTool, Description("Get all transactions made with a specific card")]
    public async Task<object> GetCardTransactions(string accountId, string cardId)
        => await transactionService.GetCardTransactionsAsync(accountId, cardId);
}
