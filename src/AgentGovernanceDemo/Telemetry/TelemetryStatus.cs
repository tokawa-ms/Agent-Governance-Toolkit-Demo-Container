// EN: Defines the immutable startup assessment shown for Azure Monitor telemetry configuration.
// JA: Azure Monitor テレメトリ構成について画面表示する不変の起動時評価を定義します。

namespace AgentGovernanceDemo.Telemetry;

/// <summary>
/// EN: Identifies whether telemetry is disabled, validly configured, or degraded.<br/>
/// JA: テレメトリが無効、正常構成、縮退状態のいずれかを識別します。
/// </summary>
public enum TelemetryState
{
    Disabled,
    Configured,
    Degraded
}

/// <summary>
/// EN: Describes the effective telemetry configuration and its user-facing status message.<br/>
/// JA: 有効なテレメトリ構成とユーザー向け状態メッセージを表します。
/// </summary>
public sealed record TelemetryStatus(
    TelemetryState State,
    string ServiceName,
    string ServiceVersion,
    string EnvironmentName,
    string Message)
{
    public bool IsConfigured => State == TelemetryState.Configured;

    public bool IsDegraded => State == TelemetryState.Degraded;
}
