using System.ComponentModel;

namespace PersonalFinance.AgentBackend.Tools;

public sealed class TransactionPlugin
{
    private readonly HttpClient _httpClient;

    public TransactionPlugin(HttpClient httpClient) => _httpClient = httpClient;

    [Description("Get the last transactions for a bank account, ordered by date")]
    public async Task<string> GetLastTransactionsAsync(
        [Description("The account ID")] string accountId,
        [Description("Number of transactions to retrieve (default 10)")] int count = 10)
    {
        var response = await _httpClient.GetAsync($"/api/transactions/{Uri.EscapeDataString(accountId)}?count={count}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Description("Search transactions by recipient/payee name for a specific account")]
    public async Task<string> GetTransactionsByRecipientNameAsync(
        [Description("The account ID")] string accountId,
        [Description("The payee/recipient name to search for")] string recipientName)
    {
        var response = await _httpClient.GetAsync(
            $"/api/transactions/{Uri.EscapeDataString(accountId)}/by-recipient/{Uri.EscapeDataString(recipientName)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Description("Get all transactions made with a specific card")]
    public async Task<string> GetCardTransactionsAsync(
        [Description("The account ID")] string accountId,
        [Description("The card ID")] string cardId)
    {
        var response = await _httpClient.GetAsync(
            $"/api/transactions/{Uri.EscapeDataString(accountId)}/card/{Uri.EscapeDataString(cardId)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
