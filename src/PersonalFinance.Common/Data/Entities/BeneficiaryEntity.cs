namespace PersonalFinance.Common.Data.Entities;

public class BeneficiaryEntity
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string BankCode { get; set; } = "";
    public string BankName { get; set; } = "";

    public AccountEntity Account { get; set; } = null!;
}
