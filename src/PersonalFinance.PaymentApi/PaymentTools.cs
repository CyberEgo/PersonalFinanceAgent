using System.ComponentModel;
using ModelContextProtocol.Server;
using PersonalFinance.Common.Models;
using PersonalFinance.PaymentApi.Services;

namespace PersonalFinance.PaymentApi;

[McpServerToolType]
public sealed class PaymentTools(IPaymentService paymentService)
{
    [McpServerTool, Description("Process a payment. Requires account_id, amount, description, recipient_name, payment_type (BankTransfer, CreditCard, DirectDebit), and optionally card_id, recipient_bank_code, category, status, idempotency_key. This tool is idempotent — calling it multiple times with the same details will NOT create duplicate payments.")]
    public async Task<object> ProcessPayment(
        string accountId,
        decimal amount,
        string description,
        string recipientName,
        string paymentType,
        string? recipientBankCode = null,
        string? cardId = null,
        string? category = null,
        string status = "pending",
        string? idempotencyKey = null)
    {
        var request = new PaymentRequest(
            AccountId: accountId,
            Amount: amount,
            Description: description,
            RecipientName: recipientName,
            RecipientBankCode: recipientBankCode,
            PaymentType: paymentType,
            CardId: cardId,
            Category: category,
            Status: status,
            IdempotencyKey: idempotencyKey);

        return await paymentService.ProcessPaymentAsync(request);
    }
}
