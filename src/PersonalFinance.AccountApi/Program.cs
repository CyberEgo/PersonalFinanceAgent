using PersonalFinance.AccountApi.Services;
using PersonalFinance.Common.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<PersonalFinanceDbContext>("personalfinancedb");

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

var app = builder.Build();

await PersonalFinanceDbSeeder.SeedAsync(app.Services);

app.MapDefaultEndpoints();

app.MapMcp("/mcp");

app.MapGet("/api/accounts/user/{userName}", async (string userName, IAccountService svc) =>
    Results.Ok(await svc.GetAccountsByUserNameAsync(userName)));

app.MapGet("/api/accounts/{accountId}", async (string accountId, IAccountService svc) =>
    await svc.GetAccountDetailsAsync(accountId) is { } account ? Results.Ok(account) : Results.NotFound());

app.MapGet("/api/accounts/{accountId}/beneficiaries", async (string accountId, IAccountService svc) =>
    Results.Ok(await svc.GetBeneficiariesAsync(accountId)));

app.MapGet("/api/accounts/{accountId}/cards", async (string accountId, IAccountService svc) =>
    Results.Ok(await svc.GetCreditCardsAsync(accountId)));

app.MapGet("/api/accounts/cards/{cardId}", async (string cardId, IAccountService svc) =>
    await svc.GetCardDetailsAsync(cardId) is { } card ? Results.Ok(card) : Results.NotFound());

app.Run();
