// EN: Applies a thread-safe sliding-window execution limit independently to each browser client.
// JA: ブラウザークライアントごとに独立したスレッドセーフなスライディングウィンドウ実行制限を適用します。

using AgentGovernanceDemo.Configuration;
using Microsoft.Extensions.Options;

namespace AgentGovernanceDemo.Integration;

/// <summary>
/// EN: Limits demo runs per client within a one-minute sliding window.<br/>
/// JA: 1 分間のスライディングウィンドウ内でクライアント単位のデモ実行回数を制限します。
/// </summary>
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

    /// <summary>
    /// EN: Attempts to reserve a run using the system clock.<br/>
    /// JA: システム時刻を使って実行枠の確保を試みます。
    /// </summary>
    public bool TryAcquire(string clientKey, out TimeSpan retryAfter) =>
        TryAcquire(clientKey, TimeProvider.System.GetUtcNow(), out retryAfter);

    /// <summary>
    /// EN: Attempts to reserve a run at a supplied time for deterministic testing.<br/>
    /// JA: 決定論的テスト向けに、指定時刻で実行枠の確保を試みます。
    /// </summary>
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
