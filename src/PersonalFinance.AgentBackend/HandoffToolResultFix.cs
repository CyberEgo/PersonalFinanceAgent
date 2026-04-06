using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace PersonalFinance.AgentBackend;

/// <summary>
/// Temporary workaround for microsoft/agent-framework#2775.
/// Wraps a handoff workflow <see cref="AIAgent"/> so that plain-string tool
/// results are serialized to <see cref="JsonElement"/> before the AG-UI
/// pipeline processes them.
/// </summary>
internal static class HandoffToolResultFix
{
    public static AIAgent CreateFixedAgent(this AIAgent innerAgent)
    {
        return new AIAgentBuilder(innerAgent)
            .Use(
                runFunc: null,
                runStreamingFunc: static (messages, session, options, inner, ct) =>
                    FixToolResults(inner.RunStreamingAsync(messages, session, options, ct)))
            .Build();
    }

    private static async IAsyncEnumerable<AgentResponseUpdate> FixToolResults(
        IAsyncEnumerable<AgentResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var update in updates.WithCancellation(ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is FunctionResultContent frc && frc.Result is string s)
                {
                    frc.Result = JsonSerializer.SerializeToElement(s);
                }
            }

            yield return update;
        }
    }
}
