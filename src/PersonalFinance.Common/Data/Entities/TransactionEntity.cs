namespace PersonalFinance.Common.Data.Entities;

public class TransactionEntity
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string FlowType { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string PaymentType { get; set; } = "";
    public string? CardId { get; set; }
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";

    public AccountEntity Account { get; set; } = null!;
}
