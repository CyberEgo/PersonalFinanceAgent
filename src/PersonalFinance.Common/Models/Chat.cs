namespace PersonalFinance.Common.Models;

public record UserProfile(
    string Email,
    string FullName,
    DateTimeOffset Timestamp);

public record ChatRequest(
    string Message,
    string? ThreadId,
    string? AttachmentId);

public record ChatStreamEvent(
    string Type,
    string? Content,
    string? AgentName,
    string? ToolName,
    string? WidgetData,
    string? Error);

public record InvoiceData(
    string? VendorName,
    string? CustomerName,
    string? InvoiceId,
    string? InvoiceDate,
    decimal? InvoiceTotal,
    string? Currency);
