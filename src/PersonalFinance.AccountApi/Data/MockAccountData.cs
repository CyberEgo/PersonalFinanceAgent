using PersonalFinance.Common.Models;

namespace PersonalFinance.AccountApi.Data;

public static class MockAccountData
{
    public static readonly List<Account> Accounts =
    [
        new(
            Id: "ACC-001",
            UserName: "arman.haeri@example.com",
            AccountHolderFullName: "Arman Haeri",
            Currency: "USD",
            Balance: 15_420.75m,
            PaymentMethods:
            [
                new("PM-001", "Visa", 12_500.00m, "****4532"),
                new("PM-002", "BankTransfer", 15_420.75m, null),
                new("PM-003", "DirectDebit", 15_420.75m, null)
            ]),
        new(
            Id: "ACC-002",
            UserName: "jane.smith@example.com",
            AccountHolderFullName: "Jane Smith",
            Currency: "USD",
            Balance: 28_150.30m,
            PaymentMethods:
            [
                new("PM-004", "Mastercard", 8_000.00m, "****7821"),
                new("PM-005", "BankTransfer", 28_150.30m, null)
            ])
    ];

    public static readonly List<Card> Cards =
    [
        new("CARD-001", "credit", "Visa Platinum", 12_500.00m, "4532-XXXX-XXXX-4532", 25_000.00m, "active"),
        new("CARD-002", "debit", "Checking Debit", 15_420.75m, "4111-XXXX-XXXX-1111", 0m, "active"),
        new("CARD-003", "credit", "Mastercard Gold", 8_000.00m, "5421-XXXX-XXXX-7821", 15_000.00m, "active")
    ];

    public static readonly List<Beneficiary> Beneficiaries =
    [
        new("BEN-001", "Electric Company Inc.", "ELEC001", "National Bank"),
        new("BEN-002", "City Water Services", "WATR001", "Metro Credit Union"),
        new("BEN-003", "Internet Provider Ltd.", "INET001", "Digital Bank"),
        new("BEN-004", "Insurance Corp.", "INSR001", "Federal Savings"),
        new("BEN-005", "Rent Payment LLC", "RENT001", "Community Bank")
    ];
}
