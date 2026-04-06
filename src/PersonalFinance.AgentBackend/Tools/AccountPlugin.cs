using System.ComponentModel;

namespace PersonalFinance.AgentBackend.Tools;

public sealed class AccountPlugin
{
    private readonly HttpClient _httpClient;

    public AccountPlugin(HttpClient httpClient) => _httpClient = httpClient;

    [Description("Get list of bank accounts for a specific user by their email/username")]
    public async Task<string> GetAccountsByUserNameAsync(
        [Description("The user's email address")] string userName)
    {
        var response = await _httpClient.GetAsync($"/api/accounts/user/{Uri.EscapeDataString(userName)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Description("Get detailed account information including balance and payment methods")]
    public async Task<string> GetAccountDetailsAsync(
        [Description("The account ID")] string accountId)
    {
        var response = await _httpClient.GetAsync($"/api/accounts/{Uri.EscapeDataString(accountId)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Description("Get list of registered beneficiaries/payees for bank transfers")]
    public async Task<string> GetRegisteredBeneficiariesAsync(
        [Description("The account ID")] string accountId)
    {
        var response = await _httpClient.GetAsync($"/api/accounts/{Uri.EscapeDataString(accountId)}/beneficiaries");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Description("Get credit cards bound to a specific bank account")]
    public async Task<string> GetCreditCardsAsync(
        [Description("The account ID")] string accountId)
    {
        var response = await _httpClient.GetAsync($"/api/accounts/{Uri.EscapeDataString(accountId)}/cards");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    [Description("Get details of a specific credit card by its ID")]
    public async Task<string> GetCardDetailsAsync(
        [Description("The card ID")] string cardId)
    {
        var response = await _httpClient.GetAsync($"/api/accounts/cards/{Uri.EscapeDataString(cardId)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
