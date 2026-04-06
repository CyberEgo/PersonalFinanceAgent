using System.Text.Json;
using System.Threading.Channels;
using PersonalFinance.AgentBackend.Services;
using PersonalFinance.Common.Models;
using ChatRequest = PersonalFinance.Common.Models.ChatRequest;
using ChatStreamEvent = PersonalFinance.Common.Models.ChatStreamEvent;

namespace PersonalFinance.AgentBackend.Hubs;

public static class ChatEndpoints
{
    private static readonly byte[] KeepaliveBytes = ": keepalive\n\n"u8.ToArray();

    public static void MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/api/chat", async (ChatRequest request, ChatOrchestrationService orchestrator, HttpContext ctx, ILogger<ChatOrchestrationService> logger) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var writer = ctx.Response.BodyWriter;
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // Channel decouples the orchestrator stream from the SSE writer so we
            // can inject keepalive comments while awaiting long tool executions.
            var channel = Channel.CreateUnbounded<ChatStreamEvent?>();

            // Producer — pushes orchestrator events into the channel
            var producerTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var evt in orchestrator.ProcessMessageStreamAsync(request, ctx.RequestAborted))
                    {
                        await channel.Writer.WriteAsync(evt, ctx.RequestAborted);
                    }
                }
                catch (OperationCanceledException) { /* client disconnected */ }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during chat stream");
                    try
                    {
                        await channel.Writer.WriteAsync(
                            new ChatStreamEvent("error", null, null, null, null, ex.Message),
                            ctx.RequestAborted);
                    }
                    catch { /* best effort */ }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });

            // Keepalive — writes null to channel every 15 s (null = SSE comment)
            using var keepaliveCts = CancellationTokenSource.CreateLinkedTokenSource(ctx.RequestAborted);
            var keepaliveTask = Task.Run(async () =>
            {
                try
                {
                    while (!keepaliveCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15), keepaliveCts.Token);
                        if (!channel.Writer.TryWrite(null)) break;
                    }
                }
                catch (OperationCanceledException) { }
            });

            // Consumer — reads from channel and writes SSE to the HTTP response
            try
            {
                await foreach (var evt in channel.Reader.ReadAllAsync(ctx.RequestAborted))
                {
                    if (evt is null)
                    {
                        await writer.WriteAsync(KeepaliveBytes, ctx.RequestAborted);
                    }
                    else
                    {
                        var json = JsonSerializer.Serialize(evt, jsonOptions);
                        var data = System.Text.Encoding.UTF8.GetBytes($"data: {json}\n\n");
                        await writer.WriteAsync(data, ctx.RequestAborted);
                    }
                    await writer.FlushAsync(ctx.RequestAborted);
                }
            }
            catch (OperationCanceledException) { /* client disconnected */ }
            finally
            {
                await keepaliveCts.CancelAsync();
            }

            await producerTask;
        });

        app.MapPost("/api/chat/sync", async (ChatRequest request, ChatOrchestrationService orchestrator) =>
        {
            var messages = new List<ChatStreamEvent>();
            await foreach (var evt in orchestrator.ProcessMessageStreamAsync(request))
            {
                messages.Add(evt);
            }
            return Results.Ok(messages);
        });
    }
}
