using AgentGovernance;
using AgentGovernance.Integration;
using AgentGovernance.Policy;

namespace AgentGovernanceDemo.Governance;

// Note 1 (EN): This service isolates Agent Governance Toolkit configuration from UI orchestration.
// Note 1 (JA): このサービスは Agent Governance Toolkit の設定を UI オーケストレーションから分離します。
public sealed class GovernanceDemoService : IDisposable
{
    // Note 2 (EN): The policy is embedded so every demo instance evaluates the same reviewable rules.
    // Note 2 (JA): すべてのデモ環境で同じ確認可能なルールを評価できるよう、ポリシーを埋め込んでいます。
    // Note 2 (EN): default_action is deny, so any tool not explicitly allowed is rejected.
    // Note 2 (JA): default_action は deny のため、明示的に許可されていないツールは拒否されます。
    public const string DefaultPolicyYaml = """
        apiVersion: governance.toolkit/v1
        version: "1.0"
        name: governance-demo-default
        description: Default-deny policy for deterministic governance demo tools.
        default_action: deny
        rules:
          - name: explicitly-deny-shell
            condition: "tool_name == 'execute_shell'"
            action: deny
            priority: 100
          - name: allow-get-weather
            condition: "tool_name == 'GetWeather'"
            action: allow
            priority: 10
          - name: allow-get-time
            condition: "tool_name == 'GetTime'"
            action: allow
            priority: 10
          - name: allow-get-location
            condition: "tool_name == 'GetLocation'"
            action: allow
            priority: 10
        """;

    public GovernanceDemoService(string? policyPath = null)
    {
        // Note 3 (EN): DenyOverrides ensures a deny rule wins if multiple policy rules conflict.
        // Note 3 (JA): DenyOverrides により、複数ルールが競合した場合は deny が必ず優先されます。
        // Note 3 (EN): Prompt-injection detection runs as part of the same pre-execution gate.
        // Note 3 (JA): prompt injection 検出も、実行前の同じガバナンスゲート内で動作します。
        Kernel = new GovernanceKernel(new GovernanceOptions
        {
            ConflictStrategy = ConflictResolutionStrategy.DenyOverrides,
            EnablePromptInjectionDetection = true,
            EnableRings = false,
            EnableCircuitBreaker = false
        });

        if (!string.IsNullOrWhiteSpace(policyPath))
        {
            // Note 4 (EN): Tests or advanced demos can provide an external policy without changing this class.
            // Note 4 (JA): テストや発展デモでは、このクラスを変更せず外部ポリシーへ差し替えられます。
            Kernel.LoadPolicy(policyPath);
        }
        else
        {
            Kernel.LoadPolicyFromYaml(DefaultPolicyYaml);
        }
    }

    public GovernanceKernel Kernel { get; }

    public ToolCallResult Evaluate(
        string agentId,
        string toolName,
        IReadOnlyDictionary<string, object>? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        // Note 5 (EN): This is the single governance-gate call used by DemoRunCoordinator.
        // Note 5 (JA): ここが DemoRunCoordinator から呼ばれる単一のガバナンスゲートです。
        // Note 5 (EN): A copied dictionary prevents callers from mutating arguments during evaluation.
        // Note 5 (JA): 引数をコピーし、評価中に呼び出し元から内容を変更されないようにしています。
        return Kernel.EvaluateToolCall(
            agentId,
            toolName,
            arguments is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(arguments));
    }

    public void Dispose() => Kernel.Dispose();
}
