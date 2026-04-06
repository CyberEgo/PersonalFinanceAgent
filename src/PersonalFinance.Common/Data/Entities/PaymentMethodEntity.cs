namespace PersonalFinance.Common.Data.Entities;

public class PaymentMethodEntity
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal AvailableBalance { get; set; }
    public string? CardNumber { get; set; }

    public AccountEntity Account { get; set; } = null!;
}
