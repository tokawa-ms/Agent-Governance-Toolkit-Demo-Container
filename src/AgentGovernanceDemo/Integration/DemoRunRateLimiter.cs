using AgentGovernanceDemo.Configuration;
using Microsoft.Extensions.Options;

namespace AgentGovernanceDemo.Integration;

public sealed class DemoRunRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly Dictionary<string, Queue<DateTimeOffset>> _runsByClient = [];
    private readonly int _limit;
    private readonly object _gate = new();

    public DemoRunRateLimiter(IOptions<DemoOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _limit = options.Value.MaxRunsPerMinute;
    }

    public bool TryAcquire(string clientKey, out TimeSpan retryAfter) =>
        TryAcquire(clientKey, TimeProvider.System.GetUtcNow(), out retryAfter);

    public bool TryAcquire(
        string clientKey,
        DateTimeOffset now,
        out TimeSpan retryAfter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientKey);

        lock (_gate)
        {
            if (!_runsByClient.TryGetValue(clientKey, out var runs))
            {
                runs = new Queue<DateTimeOffset>();
                _runsByClient.Add(clientKey, runs);
            }

            var cutoff = now - Window;
            while (runs.TryPeek(out var timestamp) && timestamp <= cutoff)
            {
                runs.Dequeue();
            }

            if (runs.Count >= _limit)
            {
                retryAfter = Window - (now - runs.Peek());
                return false;
            }

            runs.Enqueue(now);
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }
}
