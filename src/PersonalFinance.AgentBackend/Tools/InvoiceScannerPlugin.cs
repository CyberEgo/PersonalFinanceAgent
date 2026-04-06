using System.ComponentModel;
using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using PersonalFinance.AgentBackend.Services;

namespace PersonalFinance.AgentBackend.Tools;

public sealed class InvoiceScannerPlugin
{
    private readonly IFileStorageService _storage;
    private readonly DocumentIntelligenceClient? _diClient;
    private readonly ILogger<InvoiceScannerPlugin> _logger;

    public InvoiceScannerPlugin(
        IFileStorageService storage,
        DocumentIntelligenceClient? diClient,
        ILogger<InvoiceScannerPlugin> logger)
    {
        _storage = storage;
        _diClient = diClient;
        _logger = logger;
    }

    [Description("Scan an uploaded invoice or receipt image and extract structured data including vendor name, customer name, invoice ID, date, total amount, and currency.")]
    public async Task<string> ScanInvoiceAsync(
        [Description("The attachment ID of the uploaded invoice image")] string attachmentId)
    {
        if (_diClient is null)
        {
            _logger.LogWarning("Document Intelligence not configured — returning mock data for attachment {Id}", attachmentId);
            return JsonSerializer.Serialize(new
            {
                VendorName = "Electric Company Inc.",
                CustomerName = "Arman Haeri",
                InvoiceId = $"INV-{DateTime.UtcNow:yyyyMMdd}-001",
                InvoiceDate = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd"),
                InvoiceTotal = 145.80m,
                Currency = "USD",
                _note = "Mock data — configure DocumentIntelligence:Endpoint to enable real scanning"
            });
        }

        try
        {
            _logger.LogInformation("Scanning invoice from attachment {Id}", attachmentId);
            var fileBytes = await _storage.GetAsync(attachmentId);

            var operation = await _diClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-invoice",
                BinaryData.FromBytes(fileBytes));

            var result = operation.Value;
            var scanData = new Dictionary<string, object>();

            if (result.Documents is { Count: > 0 })
            {
                var doc = result.Documents[0];

                TryAdd(scanData, doc, "VendorName");
                TryAdd(scanData, doc, "VendorAddress");
                TryAdd(scanData, doc, "CustomerName");
                TryAdd(scanData, doc, "InvoiceId");
                TryAddDate(scanData, doc, "InvoiceDate");
                TryAddCurrency(scanData, doc, "InvoiceTotal");
            }

            _logger.LogInformation("Invoice scan complete: {Fields} fields extracted", scanData.Count);
            return JsonSerializer.Serialize(scanData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning invoice {Id}", attachmentId);
            return JsonSerializer.Serialize(new { error = $"Failed to scan invoice: {ex.Message}" });
        }
    }

    private static void TryAdd(Dictionary<string, object> data, AnalyzedDocument doc, string fieldName)
    {
        if (doc.Fields.TryGetValue(fieldName, out var field) && field.Content is not null)
            data[fieldName] = field.Content;
    }

    private static void TryAddDate(Dictionary<string, object> data, AnalyzedDocument doc, string fieldName)
    {
        if (doc.Fields.TryGetValue(fieldName, out var field) && field.ValueDate is not null)
            data[fieldName] = field.ValueDate.Value.ToString("yyyy-MM-dd");
        else if (field?.Content is not null)
            data[fieldName] = field.Content;
    }

    private static void TryAddCurrency(Dictionary<string, object> data, AnalyzedDocument doc, string fieldName)
    {
        if (doc.Fields.TryGetValue(fieldName, out var field) && field.ValueCurrency is not null)
        {
            data[fieldName] = field.ValueCurrency.Amount;
            data["Currency"] = field.ValueCurrency.CurrencyCode ?? "USD";
        }
        else if (field?.Content is not null)
        {
            data[fieldName] = field.Content;
        }
    }
}
