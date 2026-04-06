namespace PersonalFinance.Common.Models;

public record Account(
    string Id,
    string UserName,
    string AccountHolderFullName,
    string Currency,
    decimal Balance,
    List<PaymentMethod> PaymentMethods);

public record PaymentMethod(
    string Id,
    string Type,
    decimal AvailableBalance,
    string? CardNumber);

public record Card(
    string Id,
    string Type,
    string Name,
    decimal Balance,
    string Number,
    decimal Limit,
    string Status);

public record Beneficiary(
    string Id,
    string FullName,
    string BankCode,
    string BankName);
