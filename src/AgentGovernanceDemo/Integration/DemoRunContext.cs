// EN: Carries per-run correlation identifiers across asynchronous calls without changing method signatures.
// JA: メソッドシグネチャを変更せず、非同期呼び出し間で実行単位の相関 ID を伝播します。

namespace AgentGovernanceDemo.Integration;

/// <summary>
/// EN: Stores the audit-session and UI-subscriber identifiers for the current asynchronous run.<br/>
/// JA: 現在の非同期実行における監査セッション ID と UI 購読者 ID を保持します。
/// </summary>
public sealed record DemoRunContextState(string AuditSessionId, string SubscriberId);

/// <summary>
/// EN: Provides a disposable <see cref="AsyncLocal{T}"/> scope for demo-run correlation.<br/>
/// JA: デモ実行の相関情報を扱う破棄可能な <see cref="AsyncLocal{T}"/> スコープを提供します。
/// </summary>
public static class DemoRunContext
{
    private static readonly AsyncLocal<DemoRunContextState?> CurrentState = new();

    public static DemoRunContextState? Current => CurrentState.Value;

    /// <summary>
    /// EN: Begins a correlation scope and restores the previous scope when disposed.<br/>
    /// JA: 相関スコープを開始し、破棄時に以前のスコープを復元します。
    /// </summary>
    public static IDisposable Begin(string auditSessionId, string subscriberId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriberId);

        var previous = CurrentState.Value;
        CurrentState.Value = new DemoRunContextState(auditSessionId, subscriberId);
        return new Scope(previous);
    }

    /// <summary>
    /// EN: Restores the previous asynchronous correlation state exactly once.<br/>
    /// JA: 以前の非同期相関状態を一度だけ復元します。
    /// </summary>
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
