// EN: Defines and implements the side-effect-free tool boundary used after governance approval.
// JA: ガバナンス承認後に使用する副作用のないツール境界と実装を定義します。

namespace AgentGovernanceDemo.Governance;

/// <summary>
/// EN: Abstracts execution of the small allowlisted tool set used by the demo.<br/>
/// JA: デモで使用する小規模な許可リスト済みツール群の実行を抽象化します。
/// </summary>
public interface IDemoToolExecutor
{
    // Note 1 (EN): The interface keeps orchestration independent from the concrete safe demo tools.
    // Note 1 (JA): interface により、オーケストレーションを具体的な安全デモツールから分離します。
    ValueTask<string> ExecuteAsync(
        string toolName,
        IReadOnlyDictionary<string, object> arguments,
        CancellationToken cancellationToken);
}

// Note 2 (EN): This executor intentionally returns fixed data and never calls external systems.
// Note 2 (JA): この executor は固定データだけを返し、外部システムを一切呼び出しません。
// Note 2 (EN): It demonstrates governance flow without introducing real-world side effects.
// Note 2 (JA): 実際の副作用を持ち込まず、ガバナンスの流れだけを安全に実演できます。
/// <summary>
/// EN: Executes only predefined in-memory demo tools and rejects every unknown tool.<br/>
/// JA: 定義済みのメモリ内デモツールだけを実行し、すべての未登録ツールを拒否します。
/// </summary>
public sealed class DeterministicDemoToolExecutor : IDemoToolExecutor
{
    public ValueTask<string> ExecuteAsync(
        string toolName,
        IReadOnlyDictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Note 3 (EN): The switch is an executable allowlist, not a general-purpose plugin dispatcher.
        // Note 3 (JA): この switch は実行可能ツールの allowlist であり、汎用プラグイン機構ではありません。
        var output = toolName switch
        {
            "GetWeather" => "Seattle: 18°C, clear skies (simulated)",
            "GetTime" => "2026-01-15T12:00:00Z (simulated)",
            "GetLocation" => "Contoso Campus, Building 1 (simulated)",
            // Note 4 (EN): Defense in depth rejects unknown tools even if the gate were misconfigured.
            // Note 4 (JA): 多層防御として、ゲート設定に誤りがあっても未登録ツールをここでも拒否します。
            _ => throw new InvalidOperationException(
                $"Tool '{toolName}' has no executable demo implementation.")
        };

        return ValueTask.FromResult(output);
    }
}
