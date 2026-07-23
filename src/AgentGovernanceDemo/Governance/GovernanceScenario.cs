// EN: Defines the immutable scenario contract used to drive deterministic governance demonstrations.
// JA: 決定論的なガバナンス実演を駆動する不変シナリオ契約を定義します。

namespace AgentGovernanceDemo.Governance;

/// <summary>
/// EN: Identifies the policy result expected from a predefined demo scenario.<br/>
/// JA: 定義済みデモシナリオで期待するポリシー判断結果を識別します。
/// </summary>
public enum GovernanceExpectedOutcome
{
    Allow,
    Deny
}

/// <summary>
/// EN: Describes one safe, deterministic tool-call scenario and its expected governance result.<br/>
/// JA: 安全で決定論的な 1 つのツール呼び出しシナリオと期待されるガバナンス結果を表します。
/// </summary>
public sealed record GovernanceScenario(
    string Id,
    string Title,
    string Description,
    string ToolName,
    IReadOnlyDictionary<string, object> Arguments,
    GovernanceExpectedOutcome ExpectedOutcome);
