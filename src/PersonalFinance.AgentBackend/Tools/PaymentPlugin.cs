using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PersonalFinance.AgentBackend.Services;

namespace PersonalFinance.AgentBackend.Tools;

public sealed class PaymentPlugin
{
    private readonly HttpClient _httpClient;
    private readonly PaymentEventBroadcaster _broadcaster;

    public PaymentPlugin(HttpClient httpClient, PaymentEventBroadcaster broadcaster)
    {
        _httpClient = httpClient;
        _broadcaster = broadcaster;
    }

    [Description("Process and submit a payment. Requires account_id, amount, description, recipient_name, payment_type (BankTransfer, CreditCard, DirectDebit). Optionally card_id, recipient_bank_code, category. This tool is idempotent — calling it multiple times with the same details will NOT create duplicate payments.")]
    public async Task<string> ProcessPaymentAsync(
        [Description("The account ID to pay from")] string accountId,
        [Description("Payment amount")] decimal amount,
        [Description("Payment description")] string description,
        [Description("Name of the payment recipient")] string recipientName,
        [Description("Payment type: BankTransfer, CreditCard, or DirectDebit")] string paymentType,
        [Description("Recipient bank code (required for BankTransfer)")] string? recipientBankCode = null,
        [Description("Card ID (required for CreditCard payments)")] string? cardId = null,
        [Description("Payment category")] string? category = null)
    {
        // Generate a deterministic idempotency key from the payment details
        // so that identical tool calls produce the same key and are deduplicated.
        var idempotencyKey = GenerateIdempotencyKey(accountId, amount, description, recipientName, paymentType);

        var payload = new
        {
            AccountId = accountId,
            Amount = amount,
            Description = description,
            RecipientName = recipientName,
            RecipientBankCode = recipientBankCode,
            PaymentType = paymentType,
            CardId = cardId,
            Category = category,
            Status = "pending",
            IdempotencyKey = idempotencyKey
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/payments/process", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();

        // Notify any connected SSE clients that dashboard data has changed
        _broadcaster.NotifyPaymentCompleted(accountId);

        return result;
    }

    private static string GenerateIdempotencyKey(string accountId, decimal amount, string description, string recipientName, string paymentType)
    {
        var raw = $"{accountId}|{amount}|{description}|{recipientName}|{paymentType}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
