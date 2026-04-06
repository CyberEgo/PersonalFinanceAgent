using PersonalFinance.Common.Models;

namespace PersonalFinance.PaymentApi.Data;

public static class MockPaymentData
{
    private static readonly List<Payment> _payments = [];
    private static int _counter;

    public static Payment ProcessPayment(PaymentRequest request)
    {
        var id = $"PAY-{Interlocked.Increment(ref _counter):D4}";
        var status = request.PaymentType switch
        {
            "BankTransfer" => "pending",
            "CreditCard" => "paid",
            "DirectDebit" => "paid",
            _ => request.Status
        };

        var payment = new Payment(
            Id: id,
            AccountId: request.AccountId,
            Amount: request.Amount,
            Description: request.Description,
            Timestamp: DateTimeOffset.UtcNow,
            RecipientName: request.RecipientName,
            RecipientBankCode: request.RecipientBankCode,
            PaymentType: request.PaymentType,
            CardId: request.CardId,
            Category: request.Category,
            Status: status);

        _payments.Add(payment);
        return payment;
    }

    public static List<Payment> GetPayments() => [.. _payments];
}
