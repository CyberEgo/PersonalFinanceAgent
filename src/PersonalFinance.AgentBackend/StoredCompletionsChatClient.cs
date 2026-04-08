using Microsoft.Extensions.AI;
using ChatCompletionOptions = OpenAI.Chat.ChatCompletionOptions;

namespace PersonalFinance.AgentBackend;

/// <summary>
/// Chat client middleware that enables Azure OpenAI stored completions
/// by setting store=true on every request via RawRepresentationFactory.
/// </summary>
internal sealed class StoredCompletionsChatClient(IChatClient innerClient)
    : DelegatingChatClient(innerClient)
{
    private static ChatOptions WithStore(ChatOptions? options)
    {
        options ??= new();
        var existingFactory = options.RawRepresentationFactory;
        options.RawRepresentationFactory = client =>
        {
            var result = existingFactory?.Invoke(client) as ChatCompletionOptions ?? new ChatCompletionOptions();
            result.StoredOutputEnabled = true;
            return result;
        };
        return options;
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(messages, WithStore(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(messages, WithStore(options), cancellationToken);
}
