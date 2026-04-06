using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PersonalFinance.Common.Data.Entities;

namespace PersonalFinance.Common.Data;

public static class PersonalFinanceDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonalFinanceDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PersonalFinanceDbContext>>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Accounts.AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding database with initial data...");

        // Accounts
        var acc1 = new AccountEntity { Id = "ACC-001", UserName = "arman.haeri@example.com", AccountHolderFullName = "Arman Haeri", Currency = "USD", Balance = 15420.75m };
        var acc2 = new AccountEntity { Id = "ACC-002", UserName = "jane.smith@example.com", AccountHolderFullName = "Jane Smith", Currency = "USD", Balance = 28150.30m };
        db.Accounts.AddRange(acc1, acc2);

        // Payment methods
        db.PaymentMethods.AddRange(
            new PaymentMethodEntity { Id = "PM-001", AccountId = "ACC-001", Type = "Visa", AvailableBalance = 12500.00m, CardNumber = "****4532" },
            new PaymentMethodEntity { Id = "PM-002", AccountId = "ACC-001", Type = "BankTransfer", AvailableBalance = 15420.75m },
            new PaymentMethodEntity { Id = "PM-003", AccountId = "ACC-001", Type = "DirectDebit", AvailableBalance = 15420.75m },
            new PaymentMethodEntity { Id = "PM-004", AccountId = "ACC-002", Type = "Mastercard", AvailableBalance = 8000.00m, CardNumber = "****7821" },
            new PaymentMethodEntity { Id = "PM-005", AccountId = "ACC-002", Type = "BankTransfer", AvailableBalance = 28150.30m }
        );

        // Beneficiaries
        db.Beneficiaries.AddRange(
            new BeneficiaryEntity { Id = "BEN-001", AccountId = "ACC-001", FullName = "Electric Company Inc.", BankCode = "ELEC001", BankName = "National Bank" },
            new BeneficiaryEntity { Id = "BEN-002", AccountId = "ACC-001", FullName = "City Water Services", BankCode = "WATR001", BankName = "Metro Credit Union" },
            new BeneficiaryEntity { Id = "BEN-003", AccountId = "ACC-001", FullName = "Internet Provider Ltd.", BankCode = "INET001", BankName = "Digital Bank" },
            new BeneficiaryEntity { Id = "BEN-004", AccountId = "ACC-002", FullName = "Insurance Corp.", BankCode = "INSR001", BankName = "Federal Savings" },
            new BeneficiaryEntity { Id = "BEN-005", AccountId = "ACC-002", FullName = "Rent Payment LLC", BankCode = "RENT001", BankName = "Community Bank" }
        );

        // Cards
        db.Cards.AddRange(
            new CardEntity { Id = "CARD-001", AccountId = "ACC-001", Type = "credit", Name = "Visa Platinum", Balance = 12500.00m, Number = "4532-XXXX-XXXX-4532", Limit = 25000.00m, Status = "active" },
            new CardEntity { Id = "CARD-002", AccountId = "ACC-001", Type = "debit", Name = "Checking Debit", Balance = 15420.75m, Number = "4111-XXXX-XXXX-1111", Limit = 0.00m, Status = "active" },
            new CardEntity { Id = "CARD-003", AccountId = "ACC-002", Type = "credit", Name = "Mastercard Gold", Balance = 8000.00m, Number = "5421-XXXX-XXXX-7821", Limit = 15000.00m, Status = "active" }
        );

        // Transactions
        db.Transactions.AddRange(
            new TransactionEntity { Id = "TXN-001", AccountId = "ACC-001", Description = "Monthly Rent Payment", Type = "payment", FlowType = "outcome", RecipientName = "Rent Payment LLC", Amount = 2200.00m, Timestamp = DateTimeOffset.Parse("2026-03-30T10:00:00Z"), PaymentType = "BankTransfer", Category = "Housing", Status = "paid" },
            new TransactionEntity { Id = "TXN-002", AccountId = "ACC-001", Description = "Electric Bill - March", Type = "payment", FlowType = "outcome", RecipientName = "Electric Company Inc.", Amount = 145.80m, Timestamp = DateTimeOffset.Parse("2026-03-27T09:00:00Z"), PaymentType = "DirectDebit", Category = "Utilities", Status = "paid" },
            new TransactionEntity { Id = "TXN-003", AccountId = "ACC-001", Description = "Salary Deposit", Type = "deposit", FlowType = "income", RecipientName = "Acme Corporation", Amount = 6500.00m, Timestamp = DateTimeOffset.Parse("2026-03-25T08:00:00Z"), PaymentType = "BankTransfer", Category = "Salary", Status = "paid" },
            new TransactionEntity { Id = "TXN-004", AccountId = "ACC-001", Description = "Grocery Store", Type = "payment", FlowType = "outcome", RecipientName = "FreshMart", Amount = 87.35m, Timestamp = DateTimeOffset.Parse("2026-03-29T14:30:00Z"), PaymentType = "CreditCard", CardId = "CARD-001", Category = "Groceries", Status = "paid" },
            new TransactionEntity { Id = "TXN-005", AccountId = "ACC-001", Description = "Internet Service", Type = "payment", FlowType = "outcome", RecipientName = "Internet Provider Ltd.", Amount = 79.99m, Timestamp = DateTimeOffset.Parse("2026-03-22T11:00:00Z"), PaymentType = "DirectDebit", Category = "Utilities", Status = "paid" },
            new TransactionEntity { Id = "TXN-006", AccountId = "ACC-001", Description = "Restaurant Dinner", Type = "payment", FlowType = "outcome", RecipientName = "The Italian Place", Amount = 62.50m, Timestamp = DateTimeOffset.Parse("2026-03-31T19:00:00Z"), PaymentType = "CreditCard", CardId = "CARD-001", Category = "Dining", Status = "paid" },
            new TransactionEntity { Id = "TXN-007", AccountId = "ACC-001", Description = "Insurance Premium", Type = "payment", FlowType = "outcome", RecipientName = "Insurance Corp.", Amount = 320.00m, Timestamp = DateTimeOffset.Parse("2026-03-17T10:00:00Z"), PaymentType = "DirectDebit", Category = "Insurance", Status = "paid" },
            new TransactionEntity { Id = "TXN-008", AccountId = "ACC-001", Description = "Freelance Payment", Type = "deposit", FlowType = "income", RecipientName = "TechStartup Inc.", Amount = 1200.00m, Timestamp = DateTimeOffset.Parse("2026-03-20T15:00:00Z"), PaymentType = "BankTransfer", Category = "Freelance", Status = "paid" },
            new TransactionEntity { Id = "TXN-009", AccountId = "ACC-001", Description = "Water Bill", Type = "payment", FlowType = "outcome", RecipientName = "City Water Services", Amount = 45.20m, Timestamp = DateTimeOffset.Parse("2026-03-24T09:30:00Z"), PaymentType = "DirectDebit", Category = "Utilities", Status = "paid" },
            new TransactionEntity { Id = "TXN-010", AccountId = "ACC-001", Description = "Online Shopping", Type = "payment", FlowType = "outcome", RecipientName = "TechGadgets Store", Amount = 299.99m, Timestamp = DateTimeOffset.Parse("2026-03-28T16:00:00Z"), PaymentType = "CreditCard", CardId = "CARD-001", Category = "Shopping", Status = "paid" }
        );

        await db.SaveChangesAsync();
        logger.LogInformation("Database seeded with {AccountCount} accounts, {TxnCount} transactions.", 2, 10);
    }
}
