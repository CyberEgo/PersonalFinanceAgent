using System.ComponentModel;
using ModelContextProtocol.Server;
using PersonalFinance.AccountApi.Services;

namespace PersonalFinance.AccountApi;

[McpServerToolType]
public sealed class AccountTools(IAccountService accountService)
{
    [McpServerTool, Description("Get list of bank accounts for a specific user by their email/username")]
    public async Task<object> GetAccountsByUserName(string userName)
        => await accountService.GetAccountsByUserNameAsync(userName);

    [McpServerTool, Description("Get detailed account information including balance and payment methods")]
    public async Task<object?> GetAccountDetails(string accountId)
        => await accountService.GetAccountDetailsAsync(accountId);

    [McpServerTool, Description("Get list of registered beneficiaries/payees for bank transfers")]
    public async Task<object> GetRegisteredBeneficiaries(string accountId)
        => await accountService.GetBeneficiariesAsync(accountId);

    [McpServerTool, Description("Get credit cards bound to a specific bank account")]
    public async Task<object> GetCreditCards(string accountId)
        => await accountService.GetCreditCardsAsync(accountId);

    [McpServerTool, Description("Get details of a specific credit card by its ID")]
    public async Task<object?> GetCardDetails(string cardId)
        => await accountService.GetCardDetailsAsync(cardId);
}
