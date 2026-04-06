namespace PersonalFinance.Common.Data.Entities;

public class AccountEntity
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string AccountHolderFullName { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal Balance { get; set; }

    public List<PaymentMethodEntity> PaymentMethods { get; set; } = [];
    public List<BeneficiaryEntity> Beneficiaries { get; set; } = [];
    public List<CardEntity> Cards { get; set; } = [];
    public List<TransactionEntity> Transactions { get; set; } = [];
    public List<PaymentEntity> Payments { get; set; } = [];
}
