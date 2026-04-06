namespace PersonalFinance.Common.Data.Entities;

public class CardEntity
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Balance { get; set; }
    public string Number { get; set; } = "";
    public decimal Limit { get; set; }
    public string Status { get; set; } = "";

    public AccountEntity Account { get; set; } = null!;
}
