using System.Threading.Channels;

namespace PersonalFinance.AgentBackend.Services;

/// <summary>
/// In-process broadcast channel that notifies all connected SSE clients
/// when a payment has been processed.
/// </summary>
public sealed class PaymentEventBroadcaster
{
    private readonly Lock _lock = new();
    private readonly List<Channel<string>> _subscribers = [];

    /// <summary>Notify all connected clients that data has changed.</summary>
    public void NotifyPaymentCompleted(string accountId)
    {
        lock (_lock)
        {
            // Remove dead channels and write to live ones
            _subscribers.RemoveAll(ch =>
            {
                if (!ch.Writer.TryWrite(accountId))
                {
                    ch.Writer.TryComplete();
                    return true;
                }
                return false;
            });
        }
    }

    /// <summary>Subscribe to payment events. Dispose the returned object to unsubscribe.</summary>
    public Subscription Subscribe()
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(16)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock)
        {
            _subscribers.Add(channel);
        }

        return new Subscription(channel, () =>
        {
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }
            channel.Writer.TryComplete();
        });
    }

    public sealed class Subscription(Channel<string> channel, Action unsubscribe) : IDisposable
    {
        public ChannelReader<string> Reader => channel.Reader;
        public void Dispose() => unsubscribe();
    }
}
