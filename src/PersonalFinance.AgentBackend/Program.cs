using System.ClientModel;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using PersonalFinance.AgentBackend;
using PersonalFinance.AgentBackend.Agents;
using PersonalFinance.AgentBackend.Hubs;
using PersonalFinance.AgentBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure HTTP clients for business APIs (Aspire service discovery)
builder.Services.AddHttpClient("AccountApi", client =>
{
    client.BaseAddress = new Uri("https+http://accountapi");
});
builder.Services.AddHttpClient("TransactionApi", client =>
{
    client.BaseAddress = new Uri("https+http://transactionapi");
});
builder.Services.AddHttpClient("PaymentApi", client =>
{
    client.BaseAddress = new Uri("https+http://paymentapi");
});

// Configure Azure OpenAI via Microsoft.Extensions.AI
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required");
    var deployment = config["AzureOpenAI:DeploymentName"] ?? "gpt-4.1";
    var apiKey = config["AzureOpenAI:ApiKey"];

    AzureOpenAIClient aoaiClient;
    if (!string.IsNullOrEmpty(apiKey))
    {
        aoaiClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
    }
    else
    {
        aoaiClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
    }

    return aoaiClient
        .GetChatClient(deployment)
        .AsIChatClient()
        .AsBuilder()
        .Use(inner => new StoredCompletionsChatClient(inner))
        .UseFunctionInvocation()
        .Build();
});

// Register agent services
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// Azure Document Intelligence (optional — falls back to mock when not configured)
builder.Services.AddSingleton<DocumentIntelligenceClient?>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["DocumentIntelligence:Endpoint"];
    if (string.IsNullOrEmpty(endpoint)) return null;

    var apiKey = config["DocumentIntelligence:ApiKey"];
    return !string.IsNullOrEmpty(apiKey)
        ? new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey))
        : new DocumentIntelligenceClient(new Uri(endpoint), new DefaultAzureCredential());
});

builder.Services.AddSingleton<PaymentEventBroadcaster>();
builder.Services.AddSingleton<PersonalFinanceAgentFactory>();
builder.Services.AddScoped<ChatOrchestrationService>();

// Register SQL-backed checkpoint store
builder.Services.AddSingleton<SqlCheckpointStore>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("personalfinancedb")
        ?? throw new InvalidOperationException("ConnectionStrings:personalfinancedb is required");
    return new SqlCheckpointStore(connStr);
});

// Register multi-agent handoff workflow (required for DevUI workflow diagram)
const string agentKey = "PersonalFinanceAgent";

builder.AddWorkflow(agentKey, (sp, key) =>
{
    var agentFactory = sp.GetRequiredService<PersonalFinanceAgentFactory>();
    var chatClient = sp.GetRequiredService<IChatClient>();

    var (_, accountTools) = agentFactory.CreateAccountAgent();
    var (_, transactionTools) = agentFactory.CreateTransactionAgent();
    var (_, paymentTools) = agentFactory.CreatePaymentAgent();

    // --- Triage Agent ---
    var triageAgent = new ChatClientAgent(
        chatClient: chatClient,
        name: "triage",
        instructions: AgentInstructions.Triage);

    // --- Account Agent ---
    var accountAgent = new ChatClientAgent(
        chatClient: chatClient,
        name: "account_agent",
        instructions: AgentInstructions.Account,
        tools: accountTools);

    // --- Transaction Agent ---
    var transactionAgent = new ChatClientAgent(
        chatClient: chatClient,
        name: "transaction_agent",
        instructions: AgentInstructions.TransactionHistory,
        tools: transactionTools);

    // --- Payment Agent ---
    var paymentAgent = new ChatClientAgent(
        chatClient: chatClient,
        name: "payment_agent",
        instructions: AgentInstructions.Payment,
        tools: paymentTools);

    // Build the handoff workflow — hub-and-spoke with Triage as router
    var workflow = AgentWorkflowBuilder
        .CreateHandoffBuilderWith(triageAgent)
        .WithHandoffs(triageAgent, [accountAgent, transactionAgent, paymentAgent])
        .WithHandoff(accountAgent, triageAgent)
        .WithHandoff(transactionAgent, triageAgent)
        .WithHandoff(paymentAgent, triageAgent)
        .Build();

    return workflow.SetName(key);
});

builder.AddAIAgent(agentKey, (sp, name) =>
{
    var workflow = sp.GetRequiredKeyedService<Workflow>(agentKey);
    var checkpointStore = sp.GetRequiredService<SqlCheckpointStore>();
    var checkpointManager = CheckpointManager.CreateJson(checkpointStore);
    var execEnv = InProcessExecution.Default.WithCheckpointing(checkpointManager);
    return workflow.AsAIAgent(name: agentKey, executionEnvironment: execEnv).CreateFixedAgent();
});

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// AG-UI protocol services
builder.Services.AddAGUI();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors();

app.MapChatEndpoints();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

var personalFinanceAgent = app.Services.GetRequiredKeyedService<AIAgent>(agentKey);
app.MapAGUI(pattern: "ag-ui", aiAgent: personalFinanceAgent);

app.MapDevUI();

// File upload endpoint for invoice / receipt attachments
app.MapPost("/api/attachments/upload", async (IFormFile file, IFileStorageService storage) =>
{
    if (file.Length == 0) return Results.BadRequest(new { error = "Empty file" });
    if (file.Length > 10 * 1024 * 1024) return Results.BadRequest(new { error = "File too large (max 10 MB)" });

    await using var stream = file.OpenReadStream();
    var attachmentId = await storage.StoreAsync(file.FileName, stream);
    return Results.Ok(new { attachmentId });
}).DisableAntiforgery();

// Delete attachment (retention opt-out)
app.MapDelete("/api/attachments/{attachmentId}", async (string attachmentId, IFileStorageService storage) =>
{
    await storage.DeleteAsync(attachmentId);
    return Results.NoContent();
});

// --- Pass-through endpoints so the frontend can reach business APIs via /api ---
app.MapGet("/api/accounts/user/{userName}", async (string userName, IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("AccountApi");
    var json = await client.GetStringAsync($"/api/accounts/user/{Uri.EscapeDataString(userName)}");
    return Results.Content(json, "application/json");
});

app.MapGet("/api/accounts/{accountId}", async (string accountId, IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("AccountApi");
    var res = await client.GetAsync($"/api/accounts/{Uri.EscapeDataString(accountId)}");
    return Results.Content(await res.Content.ReadAsStringAsync(), "application/json", statusCode: (int)res.StatusCode);
});

app.MapGet("/api/accounts/{accountId}/cards", async (string accountId, IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient("AccountApi");
    var json = await client.GetStringAsync($"/api/accounts/{Uri.EscapeDataString(accountId)}/cards");
    return Results.Content(json, "application/json");
});

app.MapGet("/api/transactions/{accountId}", async (string accountId, IHttpClientFactory httpFactory, int count = 10) =>
{
    var client = httpFactory.CreateClient("TransactionApi");
    var json = await client.GetStringAsync($"/api/transactions/{Uri.EscapeDataString(accountId)}?count={count}");
    return Results.Content(json, "application/json");
});

app.MapGet("/", () => Results.Ok(new { service = "PersonalFinance Agent Backend", status = "running", framework = "Microsoft Agent Framework" }));

// SSE endpoint — clients subscribe to be notified when a payment is processed
app.MapGet("/api/events/payments", async (PaymentEventBroadcaster broadcaster, HttpContext ctx) =>
{
    ctx.Response.ContentType = "text/event-stream";
    ctx.Response.Headers.CacheControl = "no-cache";
    ctx.Response.Headers.Connection = "keep-alive";

    using var subscription = broadcaster.Subscribe();
    var writer = ctx.Response.BodyWriter;

    try
    {
        await foreach (var accountId in subscription.Reader.ReadAllAsync(ctx.RequestAborted))
        {
            var data = System.Text.Encoding.UTF8.GetBytes($"data: {{\"accountId\":\"{accountId}\"}}\n\n");
            await writer.WriteAsync(data, ctx.RequestAborted);
            await writer.FlushAsync(ctx.RequestAborted);
        }
    }
    catch (OperationCanceledException) { /* client disconnected */ }
});

await app.RunAsync();
