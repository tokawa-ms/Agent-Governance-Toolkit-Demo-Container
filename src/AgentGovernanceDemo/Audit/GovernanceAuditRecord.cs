using AgentGovernance.Audit;

namespace AgentGovernanceDemo.Audit;

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
