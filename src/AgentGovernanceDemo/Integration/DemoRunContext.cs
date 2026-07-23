namespace AgentGovernanceDemo.Integration;

public sealed record DemoRunContextState(string AuditSessionId, string SubscriberId);

public static class DemoRunContext
{
    private static readonly AsyncLocal<DemoRunContextState?> CurrentState = new();

    public static DemoRunContextState? Current => CurrentState.Value;

    public static IDisposable Begin(string auditSessionId, string subscriberId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberId);

        var previous = CurrentState.Value;
        CurrentState.Value = new DemoRunContextState(auditSessionId, subscriberId);
        return new Scope(previous);
    }

    private sealed class Scope(DemoRunContextState? previous) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                CurrentState.Value = previous;
            }
        }
    }
}
