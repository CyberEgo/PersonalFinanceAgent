using PersonalFinance.Common.Data;
using PersonalFinance.TransactionApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<PersonalFinanceDbContext>("personalfinancedb");

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapMcp("/mcp");

app.MapGet("/api/transactions/{accountId}", async (string accountId, ITransactionService svc, int count = 10) =>
    Results.Ok(await svc.GetLastTransactionsAsync(accountId, count)));

app.MapGet("/api/transactions/{accountId}/by-recipient/{recipientName}", async (string accountId, string recipientName, ITransactionService svc) =>
    Results.Ok(await svc.GetTransactionsByRecipientAsync(accountId, recipientName)));

app.MapGet("/api/transactions/{accountId}/card/{cardId}", async (string accountId, string cardId, ITransactionService svc) =>
    Results.Ok(await svc.GetCardTransactionsAsync(accountId, cardId)));

app.Run();
