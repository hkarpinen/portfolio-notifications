using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Notifications.Application.Contracts;
using Notifications.Application.Services;

namespace Infrastructure.Notifications;

/// <summary>
/// In-process, per-user notification fan-out backed by System.Threading.Channels.
/// Registered as Singleton so all scoped services share the same instance.
/// Supports multiple concurrent connections per user (e.g. multiple browser tabs).
/// </summary>
public sealed class InMemoryNotificationDispatcher : INotificationDispatcher
{
    private readonly ConcurrentDictionary<Guid, List<Channel<NotificationStreamEventDto>>> _subs = new();
    private readonly object _lock = new();

    public void Dispatch(Guid recipientUserId, NotificationStreamEventDto notification)
    {
        if (!_subs.TryGetValue(recipientUserId, out var channels)) return;

        List<Channel<NotificationStreamEventDto>> snapshot;
        lock (_lock) { snapshot = channels.ToList(); }

        foreach (var ch in snapshot)
            ch.Writer.TryWrite(notification);
    }

    public async IAsyncEnumerable<NotificationStreamEventDto> SubscribeAsync(
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<NotificationStreamEventDto>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        lock (_lock)
        {
            _subs.AddOrUpdate(
                userId,
                _ => [channel],
                (_, list) => { list.Add(channel); return list; });
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                yield return item;
        }
        finally
        {
            lock (_lock)
            {
                if (_subs.TryGetValue(userId, out var list))
                {
                    list.Remove(channel);
                    if (list.Count == 0) _subs.TryRemove(userId, out _);
                }
            }
        }
    }
}
