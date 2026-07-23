// EN: Provides bounded in-memory replay and session-scoped notifications for successfully persisted records.
// JA: 永続化に成功したレコード向けに、件数制限付きメモリ再生とセッション単位通知を提供します。

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Exposes session-scoped subscriptions and recent persisted audit records.<br/>
/// JA: セッション単位の購読と直近の永続化済み監査レコードを公開します。
/// </summary>
public interface IPersistedAuditEventHub
{
    IDisposable Subscribe(string sessionId, Action<GovernanceAuditRecord> handler);

    IReadOnlyList<GovernanceAuditRecord> GetRecent(string sessionId, int maxCount);
}

/// <summary>
/// EN: Maintains a thread-safe bounded audit cache and dispatches records to session subscribers.<br/>
/// JA: スレッドセーフな件数制限付き監査キャッシュを維持し、セッション購読者へレコードを配信します。
/// </summary>
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

    /// <summary>
    /// EN: Gets or sets an optional observer for exceptions raised by subscriber callbacks.<br/>
    /// JA: 購読者コールバックで発生した例外を監視する任意のハンドラーを取得または設定します。
    /// </summary>
    public Action<Exception>? SubscriberError { get; set; }

    /// <summary>
    /// EN: Subscribes a callback to records for one audit session.<br/>
    /// JA: 1 つの監査セッションのレコードを受け取るコールバックを購読登録します。
    /// </summary>
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

    /// <summary>
    /// EN: Returns up to the requested number of recent records for one session.<br/>
    /// JA: 1 セッションについて指定件数までの直近レコードを返します。
    /// </summary>
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

    /// <summary>
    /// EN: Owns one idempotent session subscription and unregisters it on disposal.<br/>
    /// JA: 1 件のセッション購読を所有し、破棄時に重複なく登録解除します。
    /// </summary>
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
