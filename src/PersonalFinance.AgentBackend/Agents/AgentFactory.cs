using Azure.AI.DocumentIntelligence;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using PersonalFinance.AgentBackend.Services;
using PersonalFinance.AgentBackend.Tools;

namespace PersonalFinance.AgentBackend.Agents;

public sealed class PersonalFinanceAgentFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IChatClient _chatClient;
    private readonly IFileStorageService _storage;
    private readonly DocumentIntelligenceClient? _diClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly PaymentEventBroadcaster _broadcaster;

    public PersonalFinanceAgentFactory(
        IHttpClientFactory httpClientFactory,
        IChatClient chatClient,
        IFileStorageService storage,
        ILoggerFactory loggerFactory,
        PaymentEventBroadcaster broadcaster,
        DocumentIntelligenceClient? diClient = null)
    {
        _httpClientFactory = httpClientFactory;
        _chatClient = chatClient;
        _storage = storage;
        _diClient = diClient;
        _loggerFactory = loggerFactory;
        _broadcaster = broadcaster;
    }

    public (ChatClientAgent Agent, IList<AITool> Tools) CreateTriageAgent()
    {
        var agent = new ChatClientAgent(_chatClient, AgentInstructions.Triage);
        return (agent, []);
    }

    public (ChatClientAgent Agent, IList<AITool> Tools) CreateAccountAgent()
    {
        var httpClient = _httpClientFactory.CreateClient("AccountApi");
        var plugin = new AccountPlugin(httpClient);

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(plugin.GetAccountsByUserNameAsync),
            AIFunctionFactory.Create(plugin.GetAccountDetailsAsync),
            AIFunctionFactory.Create(plugin.GetRegisteredBeneficiariesAsync),
            AIFunctionFactory.Create(plugin.GetCreditCardsAsync),
            AIFunctionFactory.Create(plugin.GetCardDetailsAsync),
        };

        var agent = new ChatClientAgent(_chatClient, AgentInstructions.Account);
        return (agent, tools);
    }

    public (ChatClientAgent Agent, IList<AITool> Tools) CreateTransactionAgent()
    {
        var accountClient = _httpClientFactory.CreateClient("AccountApi");
        var transactionClient = _httpClientFactory.CreateClient("TransactionApi");
        var accountPlugin = new AccountPlugin(accountClient);
        var transactionPlugin = new TransactionPlugin(transactionClient);

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(accountPlugin.GetAccountsByUserNameAsync),
            AIFunctionFactory.Create(accountPlugin.GetAccountDetailsAsync),
            AIFunctionFactory.Create(transactionPlugin.GetLastTransactionsAsync),
            AIFunctionFactory.Create(transactionPlugin.GetTransactionsByRecipientNameAsync),
            AIFunctionFactory.Create(transactionPlugin.GetCardTransactionsAsync),
        };

        var agent = new ChatClientAgent(_chatClient, AgentInstructions.TransactionHistory);
        return (agent, tools);
    }

    public (ChatClientAgent Agent, IList<AITool> Tools) CreatePaymentAgent()
    {
        var accountClient = _httpClientFactory.CreateClient("AccountApi");
        var transactionClient = _httpClientFactory.CreateClient("TransactionApi");
        var paymentClient = _httpClientFactory.CreateClient("PaymentApi");
        var accountPlugin = new AccountPlugin(accountClient);
        var transactionPlugin = new TransactionPlugin(transactionClient);
        var paymentPlugin = new PaymentPlugin(paymentClient, _broadcaster);
        var invoiceScanner = new InvoiceScannerPlugin(
            _storage, _diClient, _loggerFactory.CreateLogger<InvoiceScannerPlugin>());

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(accountPlugin.GetAccountsByUserNameAsync),
            AIFunctionFactory.Create(accountPlugin.GetAccountDetailsAsync),
            AIFunctionFactory.Create(accountPlugin.GetRegisteredBeneficiariesAsync),
            AIFunctionFactory.Create(transactionPlugin.GetLastTransactionsAsync),
            AIFunctionFactory.Create(paymentPlugin.ProcessPaymentAsync),
            AIFunctionFactory.Create(invoiceScanner.ScanInvoiceAsync),
        };

        var agent = new ChatClientAgent(_chatClient, AgentInstructions.Payment);
        return (agent, tools);
    }
}
