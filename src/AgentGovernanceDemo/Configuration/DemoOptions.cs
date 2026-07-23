// EN: Defines validated runtime settings that control demo pacing and per-client execution limits.
// JA: デモの進行速度とクライアント単位の実行制限を制御する検証済み実行時設定を定義します。

using System.ComponentModel.DataAnnotations;

namespace AgentGovernanceDemo.Configuration;

/// <summary>
/// EN: Provides strongly typed configuration for the interactive governance demo.<br/>
/// JA: 対話型ガバナンスデモの厳密に型付けされた構成を提供します。
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    [Range(1, 20)]
    public int MaxRunsPerMinute { get; init; } = 8;

    [Range(0, 5000)]
    public int StepDelayMilliseconds { get; init; } = 450;

    public string EnvironmentName { get; init; } = "Local";
}
