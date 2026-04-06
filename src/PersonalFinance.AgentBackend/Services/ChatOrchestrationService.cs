using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using PersonalFinance.AgentBackend.Agents;
using ChatRequest = PersonalFinance.Common.Models.ChatRequest;
using ChatStreamEvent = PersonalFinance.Common.Models.ChatStreamEvent;

namespace PersonalFinance.AgentBackend.Services;
public sealed class ChatOrchestrationService
{
    private readonly PersonalFinanceAgentFactory _agentFactory;
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatOrchestrationService> _logger;

    // Simple thread store (in production, use distributed cache or database)
    private static readonly Dictionary<string, List<ChatMessage>> _threadStore = new();
    private static readonly Dictionary<string, string> _activeAgents = new();

    public ChatOrchestrationService(
        PersonalFinanceAgentFactory agentFactory,
        IChatClient chatClient,
        ILogger<ChatOrchestrationService> logger)
    {
        _agentFactory = agentFactory;
        _chatClient = chatClient;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> ProcessMessageStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var threadId = request.ThreadId ?? Guid.NewGuid().ToString("N");

        if (!_threadStore.TryGetValue(threadId, out var history))
        {
            history = [new(ChatRole.System, "User profile: arman.haeri@example.com (Arman Haeri)")];
            _threadStore[threadId] = history;
        }

        // Add user message
        var userMessage = request.Message;
        if (request.AttachmentId is not null)
        {
            userMessage += $" [attachment_id: {request.AttachmentId}]";
        }
        history.Add(new(ChatRole.User, userMessage));

        // Determine which agent should handle this
        var currentAgent = await RouteToAgentAsync(threadId, history, cancellationToken);

        yield return new ChatStreamEvent("thread_id", threadId, null, null, null, null);
        yield return new ChatStreamEvent("agent", null, currentAgent, null, null, null);

        // Get agent and tools
        var (agent, tools) = currentAgent switch
        {
            "AccountAgent" => _agentFactory.CreateAccountAgent(),
            "TransactionHistoryAgent" => _agentFactory.CreateTransactionAgent(),
            "PaymentAgent" => _agentFactory.CreatePaymentAgent(),
            _ => _agentFactory.CreateTriageAgent()
        };

        // Build messages with agent system prompt
        var agentMessages = new List<ChatMessage>
        {
            new(ChatRole.System, agent.Instructions ?? "")
        };
        agentMessages.AddRange(history);

        // Run agent with tools via streaming
        var chatOptions = new ChatOptions
        {
            Temperature = 0f,
            TopP = 0.1f
        };
        if (tools.Count > 0)
        {
            chatOptions.Tools = tools;
        }

        var fullResponse = new System.Text.StringBuilder();
        var reportedToolCalls = new HashSet<string>();

        // FunctionInvokingChatClient yields streaming updates from ALL rounds of
        // function-calling.  Intermediate rounds (where the model emits text *and*
        // tool calls) produce text that will be superseded by the next round's
        // response, leading to duplicate content on the client.
        //
        // Strategy: buffer text per round.  When we detect a tool call we know the
        // current round is intermediate — discard its buffered text and emit a
        // "clear" event so the client can reset the displayed content.  Only stream
        // text from rounds that contain no tool calls (the final round).
        var roundBuffer = new System.Text.StringBuilder();
        var roundHasToolCalls = false;

        await foreach (var update in _chatClient.GetStreamingResponseAsync(agentMessages, chatOptions, cancellationToken))
        {
            // If we previously saw tool calls and now receive new text, a new
            // round has started — reset tracking.
            if (roundHasToolCalls && !string.IsNullOrEmpty(update.Text))
            {
                roundBuffer.Clear();
                roundHasToolCalls = false;
            }

            // Detect tool/function calls in the streaming content
            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent fc && reportedToolCalls.Add(fc.CallId ?? fc.Name))
                {
                    // If text was already streamed for this round, tell the client
                    // to discard it — this round is intermediate.
                    if (!roundHasToolCalls && roundBuffer.Length > 0)
                    {
                        yield return new ChatStreamEvent("clear", null, null, null, null, null);
                    }

                    _logger.LogInformation("Tool call: {Tool}", fc.Name);
                    yield return new ChatStreamEvent("tool_call", null, currentAgent, fc.Name, null, null);
                    roundHasToolCalls = true;
                }
            }

            if (!string.IsNullOrEmpty(update.Text))
            {
                roundBuffer.Append(update.Text);

                if (!roundHasToolCalls)
                {
                    // No tool calls seen yet in this round — stream text to client
                    yield return new ChatStreamEvent("delta", update.Text, currentAgent, null, null, null);
                }
            }
        }

        // Assemble final response — only the last round's text
        fullResponse.Append(roundBuffer);

        // Add assistant response to history
        history.Add(new(ChatRole.Assistant, fullResponse.ToString()));

        yield return new ChatStreamEvent("done", null, currentAgent, null, null, null);
    }

    private async Task<string> RouteToAgentAsync(
        string threadId,
        List<ChatMessage> history,
        CancellationToken cancellationToken)
    {
        // If we already have an active specialist, keep routing there
        if (_activeAgents.TryGetValue(threadId, out var activeAgent) && activeAgent != "TriageAgent")
        {
            return activeAgent;
        }

        // Use chat client to classify the request
        var classificationMessages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                Classify the user's banking request into exactly one category. Respond with ONLY the category name:
                - AccountAgent: account balance, payment methods, cards, beneficiaries
                - TransactionHistoryAgent: transaction history, movements, payment history
                - PaymentAgent: make a payment, upload invoice/bill, process payment
                - TriageAgent: unclear or unrelated
                """)
        };

        // Add the last user message for classification
        var lastUserMessage = history.LastOrDefault(m => m.Role == ChatRole.User);
        if (lastUserMessage is not null)
        {
            classificationMessages.Add(new(ChatRole.User, lastUserMessage.Text ?? ""));
        }

        var classificationOptions = new ChatOptions { Temperature = 0f, TopP = 0.1f };
        var result = await _chatClient.GetResponseAsync(classificationMessages, classificationOptions, cancellationToken);
        var agentName = result.Text?.Trim() ?? "TriageAgent";

        // Normalize the agent name
        agentName = agentName switch
        {
            var a when a.Contains("Account", StringComparison.OrdinalIgnoreCase) => "AccountAgent",
            var a when a.Contains("Transaction", StringComparison.OrdinalIgnoreCase) => "TransactionHistoryAgent",
            var a when a.Contains("Payment", StringComparison.OrdinalIgnoreCase) => "PaymentAgent",
            _ => "TriageAgent"
        };

        _activeAgents[threadId] = agentName;
        _logger.LogInformation("Routed thread {ThreadId} to {AgentName}", threadId, agentName);

        return agentName;
    }
}
