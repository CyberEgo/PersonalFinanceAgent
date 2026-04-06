namespace PersonalFinance.Common.Models;

public record Transaction(
    string Id,
    string Description,
    string Type,
    string FlowType,
    string RecipientName,
    decimal Amount,
    DateTimeOffset Timestamp,
    string PaymentType,
    string? CardId,
    string Category,
    string Status);
