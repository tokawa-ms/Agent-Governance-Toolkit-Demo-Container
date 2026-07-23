// EN: Defines the sanitized, storage-safe representation of a governance audit event.
// JA: サニタイズ済みでストレージへ安全に保存できるガバナンス監査イベント表現を定義します。

using AgentGovernance.Audit;

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Represents one immutable audit record persisted in JSONL format.<br/>
/// JA: JSONL 形式で永続化される 1 件の不変監査レコードを表します。
/// </summary>
public sealed record GovernanceAuditRecord
{
    public required string EventId { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required GovernanceEventType Type { get; init; }

    public required string AgentId { get; init; }

    public required string SessionId { get; init; }

    public string? PolicyName { get; init; }

    public required Dictionary<string, object?> Data { get; init; }
}
