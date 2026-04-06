using Microsoft.EntityFrameworkCore;
using PersonalFinance.Common.Data;
using PersonalFinance.Common.Models;

namespace PersonalFinance.AccountApi.Services;

public interface IAccountService
{
    Task<List<Account>> GetAccountsByUserNameAsync(string userName);
    Task<Account?> GetAccountDetailsAsync(string accountId);
    Task<List<Beneficiary>> GetBeneficiariesAsync(string accountId);
    Task<List<Card>> GetCreditCardsAsync(string accountId);
    Task<Card?> GetCardDetailsAsync(string cardId);
}

public class AccountService(PersonalFinanceDbContext db) : IAccountService
{
    public async Task<List<Account>> GetAccountsByUserNameAsync(string userName)
    {
        var accounts = await db.Accounts
            .Include(a => a.PaymentMethods)
            .Where(a => a.UserName == userName)
            .ToListAsync();

        return accounts.Select(a => new Account(
            a.Id, a.UserName, a.AccountHolderFullName, a.Currency, a.Balance,
            a.PaymentMethods.Select(pm => new PaymentMethod(pm.Id, pm.Type, pm.AvailableBalance, pm.CardNumber)).ToList()
        )).ToList();
    }

    public async Task<Account?> GetAccountDetailsAsync(string accountId)
    {
        var a = await db.Accounts
            .Include(a => a.PaymentMethods)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (a is null) return null;

        return new Account(
            a.Id, a.UserName, a.AccountHolderFullName, a.Currency, a.Balance,
            a.PaymentMethods.Select(pm => new PaymentMethod(pm.Id, pm.Type, pm.AvailableBalance, pm.CardNumber)).ToList()
        );
    }

    public async Task<List<Beneficiary>> GetBeneficiariesAsync(string accountId)
        => await db.Beneficiaries
            .Where(b => b.AccountId == accountId)
            .Select(b => new Beneficiary(b.Id, b.FullName, b.BankCode, b.BankName))
            .ToListAsync();

    public async Task<List<Card>> GetCreditCardsAsync(string accountId)
        => await db.Cards
            .Where(c => c.AccountId == accountId)
            .Select(c => new Card(c.Id, c.Type, c.Name, c.Balance, c.Number, c.Limit, c.Status))
            .ToListAsync();

    public async Task<Card?> GetCardDetailsAsync(string cardId)
    {
        var c = await db.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
        return c is null ? null : new Card(c.Id, c.Type, c.Name, c.Balance, c.Number, c.Limit, c.Status);
    }
}
