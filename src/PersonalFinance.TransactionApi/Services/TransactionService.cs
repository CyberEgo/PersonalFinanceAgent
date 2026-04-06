using Microsoft.EntityFrameworkCore;
using PersonalFinance.Common.Data;
using PersonalFinance.Common.Models;

namespace PersonalFinance.TransactionApi.Services;

public interface ITransactionService
{
    Task<List<Transaction>> GetLastTransactionsAsync(string accountId, int count = 10);
    Task<List<Transaction>> GetTransactionsByRecipientAsync(string accountId, string recipientName);
    Task<List<Transaction>> GetCardTransactionsAsync(string accountId, string cardId);
}

public class TransactionService(PersonalFinanceDbContext db) : ITransactionService
{
    public async Task<List<Transaction>> GetLastTransactionsAsync(string accountId, int count = 10)
        => await db.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Timestamp)
            .Take(count)
            .Select(t => new Transaction(t.Id, t.Description, t.Type, t.FlowType, t.RecipientName, t.Amount, t.Timestamp, t.PaymentType, t.CardId, t.Category, t.Status))
            .ToListAsync();

    public async Task<List<Transaction>> GetTransactionsByRecipientAsync(string accountId, string recipientName)
        => await db.Transactions
            .Where(t => t.AccountId == accountId && t.RecipientName.Contains(recipientName))
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new Transaction(t.Id, t.Description, t.Type, t.FlowType, t.RecipientName, t.Amount, t.Timestamp, t.PaymentType, t.CardId, t.Category, t.Status))
            .ToListAsync();

    public async Task<List<Transaction>> GetCardTransactionsAsync(string accountId, string cardId)
        => await db.Transactions
            .Where(t => t.AccountId == accountId && t.CardId == cardId)
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new Transaction(t.Id, t.Description, t.Type, t.FlowType, t.RecipientName, t.Amount, t.Timestamp, t.PaymentType, t.CardId, t.Category, t.Status))
            .ToListAsync();
}
