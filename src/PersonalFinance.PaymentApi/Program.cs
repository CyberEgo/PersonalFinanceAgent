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

app.MapPost("/api/payments/process", async (PaymentRequest request, IPaymentService svc, ILogger<Program> logger) =>
{
    try
    {
        var result = await svc.ProcessPaymentAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Payment processing failed for AccountId={AccountId}, Amount={Amount}, PaymentType={PaymentType}, CardId={CardId}",
            request.AccountId, request.Amount, request.PaymentType, request.CardId);
        return Results.Problem(
            detail: ex.Message,
            title: "Payment processing failed",
            statusCode: 500);
    }
});

app.MapGet("/api/payments", async (IPaymentService svc) =>
    Results.Ok(await svc.GetPaymentsAsync()));

app.MapGet("/api/payments/invoice/{invoiceId}", async (string invoiceId, IPaymentService svc) =>
{
    var payment = await svc.GetPaymentByInvoiceIdAsync(invoiceId);
    return payment is not null ? Results.Ok(payment) : Results.NotFound();
});

app.Run();
