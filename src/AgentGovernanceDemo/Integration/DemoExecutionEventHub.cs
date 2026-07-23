using AgentGovernanceDemo.Configuration;
using AgentGovernanceDemo.Governance;
using Microsoft.Extensions.Options;

namespace AgentGovernanceDemo.Integration;

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

    private sealed class Subscription(DemoExecutionEventHub owner, string subscriberId) : IDisposable
    {
        private DemoExecutionEventHub? _owner = owner;

        public void Dispose() =>
            Interlocked.Exchange(ref _owner, null)?.Unsubscribe(subscriberId);
    }
}
