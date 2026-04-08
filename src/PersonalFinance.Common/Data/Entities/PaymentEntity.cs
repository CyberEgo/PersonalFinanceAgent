namespace PersonalFinance.Common.Data.Entities;

public class PaymentEntity
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string RecipientName { get; set; } = "";
    public string? RecipientBankCode { get; set; }
    public string PaymentType { get; set; } = "";
    public string? CardId { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "";
    public string? IdempotencyKey { get; set; }
    public string? InvoiceId { get; set; }

    public AccountEntity Account { get; set; } = null!;
}
