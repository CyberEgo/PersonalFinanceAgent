using PersonalFinance.Common.Data;
using PersonalFinance.Common.Models;
using PersonalFinance.PaymentApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<PersonalFinanceDbContext>("personalfinancedb");

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapMcp("/mcp");

app.MapPost("/api/payments/process", async (PaymentRequest request, IPaymentService svc) =>
    Results.Ok(await svc.ProcessPaymentAsync(request)));

app.MapGet("/api/payments", async (IPaymentService svc) =>
    Results.Ok(await svc.GetPaymentsAsync()));

app.Run();
