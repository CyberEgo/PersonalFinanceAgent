using PersonalFinance.Common.Models;

namespace PersonalFinance.TransactionApi.Data;

public static class MockTransactionData
{
    public static readonly List<Transaction> Transactions =
    [
        new("TXN-001", "Monthly Rent Payment", "payment", "outcome", "Rent Payment LLC",
            2_200.00m, DateTimeOffset.Now.AddDays(-2), "BankTransfer", null, "Housing", "paid"),
        new("TXN-002", "Electric Bill - March", "payment", "outcome", "Electric Company Inc.",
            145.80m, DateTimeOffset.Now.AddDays(-5), "DirectDebit", null, "Utilities", "paid"),
        new("TXN-003", "Salary Deposit", "deposit", "income", "Acme Corporation",
            6_500.00m, DateTimeOffset.Now.AddDays(-7), "BankTransfer", null, "Salary", "paid"),
        new("TXN-004", "Grocery Store", "payment", "outcome", "FreshMart",
            87.35m, DateTimeOffset.Now.AddDays(-3), "CreditCard", "CARD-001", "Groceries", "paid"),
        new("TXN-005", "Internet Service", "payment", "outcome", "Internet Provider Ltd.",
            79.99m, DateTimeOffset.Now.AddDays(-10), "DirectDebit", null, "Utilities", "paid"),
        new("TXN-006", "Restaurant Dinner", "payment", "outcome", "The Italian Place",
            62.50m, DateTimeOffset.Now.AddDays(-1), "CreditCard", "CARD-001", "Dining", "paid"),
        new("TXN-007", "Insurance Premium", "payment", "outcome", "Insurance Corp.",
            320.00m, DateTimeOffset.Now.AddDays(-15), "DirectDebit", null, "Insurance", "paid"),
        new("TXN-008", "Freelance Payment", "deposit", "income", "TechStartup Inc.",
            1_200.00m, DateTimeOffset.Now.AddDays(-12), "BankTransfer", null, "Freelance", "paid"),
        new("TXN-009", "Water Bill", "payment", "outcome", "City Water Services",
            45.20m, DateTimeOffset.Now.AddDays(-8), "DirectDebit", null, "Utilities", "paid"),
        new("TXN-010", "Online Shopping", "payment", "outcome", "TechGadgets Store",
            299.99m, DateTimeOffset.Now.AddDays(-4), "CreditCard", "CARD-001", "Shopping", "paid")
    ];
}
