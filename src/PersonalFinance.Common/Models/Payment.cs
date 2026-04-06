namespace PersonalFinance.Common.Models;

public record Payment(
    string? Id,
    string AccountId,
    decimal Amount,
    string Description,
    DateTimeOffset Timestamp,
    string RecipientName,
    string? RecipientBankCode,
    string PaymentType,
    string? CardId,
    string? Category,
    string Status);

public record PaymentRequest(
    string AccountId,
    decimal Amount,
    string Description,
    string RecipientName,
    string? RecipientBankCode,
    string PaymentType,
    string? CardId,
    string? Category,
    string Status,
    string? IdempotencyKey = null);
