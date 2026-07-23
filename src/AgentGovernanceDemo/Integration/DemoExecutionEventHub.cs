// EN: Routes live governance stage events to the browser session that initiated a run.
// JA: ライブのガバナンス段階イベントを、実行を開始したブラウザーセッションへ配信します。

using AgentGovernanceDemo.Configuration;
using AgentGovernanceDemo.Governance;
using Microsoft.Extensions.Options;

namespace AgentGovernanceDemo.Integration;

/// <summary>
/// EN: Dispatches execution events to session-scoped UI subscribers and controls demo pacing.<br/>
/// JA: 実行イベントをセッション単位の UI 購読者へ配信し、デモの進行速度を制御します。
/// </summary>
public sealed class DemoExecutionEventHub(IOptions<DemoOptions> options) : IGovernanceDemoEventSink
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Action<GovernanceDemoEvent>> _subscribers =
        new(StringComparer.Ordinal);
    private readonly int _stepDelayMilliseconds = options.Value.StepDelayMilliseconds;

    public IDisposable Subscribe(string subscriberId, Action<GovernanceDemoEvent> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberId);
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            _subscribers[subscriberId] = handler;
        }

        return new Subscription(this, subscriberId);
    }

    public async ValueTask PublishAsync(
        GovernanceDemoEvent governanceEvent,
        CancellationToken cancellationToken)
    {
        var subscriberId = DemoRunContext.Current?.SubscriberId;
        Action<GovernanceDemoEvent>? handler = null;
        if (subscriberId is not null)
        {
            lock (_gate)
            {
                _subscribers.TryGetValue(subscriberId, out handler);
            }
        }

        handler?.Invoke(governanceEvent);

        if (_stepDelayMilliseconds > 0)
        {
            await Task.Delay(_stepDelayMilliseconds, cancellationToken);
        }
    }

    private void Unsubscribe(string subscriberId)
    {
        lock (_gate)
        {
            _subscribers.Remove(subscriberId);
        }
    }

    /// <summary>
    /// EN: Removes one UI subscriber when its component scope is disposed.<br/>
    /// JA: コンポーネントスコープの破棄時に 1 件の UI 購読者を削除します。
    /// </summary>
    private sealed class Subscription(DemoExecutionEventHub owner, string subscriberId) : IDisposable
    {
        private DemoExecutionEventHub? _owner = owner;

        public void Dispose() =>
            Interlocked.Exchange(ref _owner, null)?.Unsubscribe(subscriberId);
    }
}
