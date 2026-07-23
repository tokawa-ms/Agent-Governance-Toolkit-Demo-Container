namespace AgentGovernanceDemo.Audit;

public interface IPersistedAuditEventHub
{
    IDisposable Subscribe(string sessionId, Action<GovernanceAuditRecord> handler);

    IReadOnlyList<GovernanceAuditRecord> GetRecent(string sessionId, int maxCount);
}

public sealed class PersistedAuditEventHub : IPersistedAuditEventHub, IDisposable
{
    private readonly object _gate = new();
    private readonly int _capacity;
    private readonly LinkedList<GovernanceAuditRecord> _records = [];
    private readonly Dictionary<string, List<Action<GovernanceAuditRecord>>> _subscribers =
        new(StringComparer.Ordinal);
    private bool _disposed;

    public PersistedAuditEventHub(int capacity = 1_000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public Action<Exception>? SubscriberError { get; set; }

    public IDisposable Subscribe(string sessionId, Action<GovernanceAuditRecord> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_subscribers.TryGetValue(sessionId, out var handlers))
            {
                handlers = [];
                _subscribers.Add(sessionId, handlers);
            }

            handlers.Add(handler);
        }

        return new Subscription(this, sessionId, handler);
    }

    public IReadOnlyList<GovernanceAuditRecord> GetRecent(string sessionId, int maxCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _records
                .Reverse()
                .Where(record => string.Equals(record.SessionId, sessionId, StringComparison.Ordinal))
                .Take(maxCount)
                .Reverse()
                .ToArray();
        }
    }

    internal void Publish(GovernanceAuditRecord record)
    {
        Action<GovernanceAuditRecord>[] handlers;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _records.AddLast(record);
            while (_records.Count > _capacity)
            {
                _records.RemoveFirst();
            }

            handlers = _subscribers.TryGetValue(record.SessionId, out var subscriptions)
                ? subscriptions.ToArray()
                : [];
        }

        foreach (var handler in handlers)
        {
            try
            {
                handler(record);
            }
            catch (Exception exception)
            {
                SubscriberError?.Invoke(exception);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _records.Clear();
            _subscribers.Clear();
        }
    }

    private void Unsubscribe(string sessionId, Action<GovernanceAuditRecord> handler)
    {
        lock (_gate)
        {
            if (_disposed || !_subscribers.TryGetValue(sessionId, out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _subscribers.Remove(sessionId);
            }
        }
    }

    private sealed class Subscription(
        PersistedAuditEventHub owner,
        string sessionId,
        Action<GovernanceAuditRecord> handler) : IDisposable
    {
        private PersistedAuditEventHub? _owner = owner;

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.Unsubscribe(sessionId, handler);
        }
    }
}
