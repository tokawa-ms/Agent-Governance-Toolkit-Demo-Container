// EN: Builds the explainable source-code path shown beneath the execution flow.
// JA: 実行フロー下部に表示する説明可能なソースコードパスを構築します。

using AgentGovernanceDemo.Governance;

namespace AgentGovernanceDemo.Presentation;

public enum GovernanceCodePointStatus
{
    Pending,
    Active,
    Allowed,
    Blocked,
    NotTaken,
    Failed
}

public sealed record GovernanceCodeLine(
    string Text,
    string? PointId = null);

public sealed record GovernanceCodeSnippet(
    string Title,
    string SourcePath,
    IReadOnlyList<GovernanceCodeLine> Lines);

public sealed record GovernanceCodePoint(
    string Id,
    string Label,
    string Description,
    GovernanceCodePointStatus Status);

public sealed record GovernancePolicyLine(
    int Number,
    string Text,
    GovernanceCodePointStatus Status);

public sealed class GovernanceCodePathState
{
    private static readonly IReadOnlyList<GovernanceCodeSnippet> Snippets =
    [
        new(
            "1. ガバナンス評価",
            "Governance/DemoRunCoordinator.cs → Governance/GovernanceDemoService.cs",
            [
                new("var decision = _governance.Evaluate(", "coordinator-evaluate"),
                new("    AgentId, scenario.ToolName, scenario.Arguments);"),
                new(""),
                new("return Kernel.EvaluateToolCall(", "kernel-evaluate"),
                new("    agentId,"),
                new("    toolName,"),
                new("    arguments is null"),
                new("        ? new Dictionary<string, object>()"),
                new("        : new Dictionary<string, object>(arguments));")
            ]),
        new(
            "2. 拒否時のブロック処理",
            "Governance/DemoRunCoordinator.cs",
            [
                new("if (!decision.Allowed)", "deny-branch"),
                new("{"),
                new("    await AddStepAsync("),
                new("        steps,"),
                new("        sessionId,"),
                new("        scenario,"),
                new("        DemoRunStepKind.ToolExecution,"),
                new("        DemoRunStepStatus.Skipped,", "execution-skipped"),
                new("        \"Tool execution skipped\","),
                new("        \"Denied tools are never invoked.\","),
                new("        cancellationToken);"),
                new(""),
                new("    return new DemoRunState(", "early-return"),
                new("        sessionId,"),
                new("        runSequence,"),
                new("        scenario,"),
                new("        DemoRunStatus.Denied,"),
                new("        steps.AsReadOnly(),"),
                new("        null,"),
                new("        decision.Reason,"),
                new("        decisionDetails);"),
                new("}")
            ])
    ];

    private GovernanceCodePathState(
        GovernanceGateKind gateKind,
        string toolName,
        string? matchedRule,
        string summary,
        IReadOnlyList<GovernanceCodePoint> points,
        string policySourcePath,
        IReadOnlyList<GovernancePolicyLine> policyLines,
        string policyDecisionSummary,
        string matchedCondition)
    {
        GateKind = gateKind;
        ToolName = toolName;
        MatchedRule = matchedRule;
        Summary = summary;
        Points = points;
        PolicySourcePath = policySourcePath;
        PolicyLines = policyLines;
        PolicyDecisionSummary = policyDecisionSummary;
        MatchedCondition = matchedCondition;
    }

    public GovernanceGateKind GateKind { get; }

    public string GateLabel => GateKind switch
    {
        GovernanceGateKind.AllowlistRule => "Allowlist ルール",
        GovernanceGateKind.ExplicitDenyRule => "明示的 deny ルール",
        GovernanceGateKind.DefaultDeny => "Default deny",
        GovernanceGateKind.PromptInjectionDetection => "Prompt injection 検出",
        _ => "不明なゲート"
    };

    public string ToolName { get; }

    public string? MatchedRule { get; }

    public string RuleLabel => MatchedRule
        ?? (GateKind switch
        {
            GovernanceGateKind.DefaultDeny => "default_action: deny",
            GovernanceGateKind.PromptInjectionDetection => "組み込み prompt-injection detector",
            _ => "実行後に確定"
        });

    public string Summary { get; }

    public IReadOnlyList<GovernanceCodePoint> Points { get; }

    public IReadOnlyList<GovernanceCodeSnippet> CodeSnippets => Snippets;

    public string PolicySourcePath { get; }

    public IReadOnlyList<GovernancePolicyLine> PolicyLines { get; }

    public string PolicyDecisionSummary { get; }

    public string MatchedCondition { get; }

    public GovernanceCodePointStatus PolicyDecisionStatus =>
        Points[0].Status;

    public GovernanceCodePointStatus StatusFor(string? pointId) =>
        pointId is null
            ? GovernanceCodePointStatus.Pending
            : Points.First(point => point.Id == pointId).Status;

    public static GovernanceCodePathState Create(
        GovernanceScenario scenario,
        ExecutionFlowState flow,
        DemoRunStatus? runStatus,
        GovernancePolicyDefinition? policyDefinition = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(flow);

        policyDefinition ??= new GovernancePolicyDefinition(
            "組み込み default policy",
            GovernanceDemoService.DefaultPolicyYaml);

        var gateKind = flow.Decision?.GateKind ?? ExpectedGateFor(scenario);
        var evaluationStatus = EvaluationStatus(flow, runStatus);
        var denyPathStatus = DenyPathStatus(flow, runStatus, evaluationStatus);
        var summary = SummaryFor(gateKind, scenario.ToolName, runStatus, evaluationStatus);
        var policyLines = CreatePolicyLines(
            policyDefinition.Yaml,
            gateKind,
            flow.Decision?.MatchedRule,
            evaluationStatus);

        return new GovernanceCodePathState(
            gateKind,
            scenario.ToolName,
            flow.Decision?.MatchedRule,
            summary,
            [
                new(
                    "coordinator-evaluate",
                    "評価要求",
                    "選択されたツール名と引数を単一のガバナンス境界へ渡します。",
                    evaluationStatus),
                new(
                    "kernel-evaluate",
                    "Toolkit ゲート",
                    "ポリシーと prompt injection 検出をツール実行前に評価します。",
                    evaluationStatus),
                new(
                    "deny-branch",
                    "deny 分岐",
                    "Allowed が false の場合だけブロック処理へ進みます。",
                    denyPathStatus),
                new(
                    "execution-skipped",
                    "実行を Skipped に記録",
                    "ツールを呼び出さず、未実行であることを明示します。",
                    denyPathStatus),
                new(
                    "early-return",
                    "早期 return",
                    "許可後のツール実行境界へ到達する前に処理を終了します。",
                    denyPathStatus)
            ],
            policyDefinition.SourcePath,
            policyLines,
            PolicyDecisionSummaryFor(
                gateKind,
                scenario.ToolName,
                flow.Decision?.MatchedRule,
                runStatus,
                evaluationStatus),
            MatchedConditionFor(
                policyLines,
                gateKind,
                flow.Decision?.MatchedRule,
                runStatus));
    }

    private static IReadOnlyList<GovernancePolicyLine> CreatePolicyLines(
        string yaml,
        GovernanceGateKind gateKind,
        string? matchedRule,
        GovernanceCodePointStatus evaluationStatus)
    {
        var lines = yaml.ReplaceLineEndings("\n").Split('\n');
        var highlighted = HighlightedLineNumbers(lines, gateKind, matchedRule);

        return lines
            .Select(
                (line, index) => new GovernancePolicyLine(
                    index + 1,
                    line,
                    highlighted.Contains(index)
                        ? evaluationStatus
                        : GovernanceCodePointStatus.Pending))
            .ToArray();
    }

    private static HashSet<int> HighlightedLineNumbers(
        IReadOnlyList<string> lines,
        GovernanceGateKind gateKind,
        string? matchedRule)
    {
        if (gateKind == GovernanceGateKind.PromptInjectionDetection)
        {
            return [];
        }

        if (gateKind == GovernanceGateKind.DefaultDeny)
        {
            return lines
                .Select((line, index) => (line, index))
                .Where(item => item.line.TrimStart().StartsWith("default_action:", StringComparison.Ordinal))
                .Select(item => item.index)
                .ToHashSet();
        }

        if (string.IsNullOrWhiteSpace(matchedRule))
        {
            return [];
        }

        var ruleStart = Enumerable.Range(0, lines.Count)
            .FirstOrDefault(
                index => lines[index].Trim().Equals(
                    $"- name: {matchedRule}",
                    StringComparison.Ordinal));

        if (!lines[ruleStart].Trim().Equals($"- name: {matchedRule}", StringComparison.Ordinal))
        {
            return [];
        }

        var ruleEnd = Enumerable.Range(ruleStart + 1, lines.Count - ruleStart - 1)
            .FirstOrDefault(
                index => lines[index].TrimStart().StartsWith("- name:", StringComparison.Ordinal),
                lines.Count);

        return Enumerable.Range(ruleStart, ruleEnd - ruleStart).ToHashSet();
    }

    private static string MatchedConditionFor(
        IReadOnlyList<GovernancePolicyLine> policyLines,
        GovernanceGateKind gateKind,
        string? matchedRule,
        DemoRunStatus? runStatus)
    {
        if (runStatus is null)
        {
            return "実行後に合致条件を表示します。";
        }

        if (gateKind == GovernanceGateKind.PromptInjectionDetection)
        {
            return "組み込み prompt-injection detector（YAML ルール評価前）";
        }

        if (gateKind == GovernanceGateKind.DefaultDeny)
        {
            return "rules に合致する条件なし → default_action: deny";
        }

        var condition = policyLines
            .Where(line => line.Status != GovernanceCodePointStatus.Pending)
            .Select(line => line.Text.Trim())
            .FirstOrDefault(line => line.StartsWith("condition:", StringComparison.Ordinal));

        return string.IsNullOrWhiteSpace(condition)
            ? matchedRule ?? "合致条件なし"
            : $"{matchedRule}: {condition["condition:".Length..].Trim()}";
    }

    private static string PolicyDecisionSummaryFor(
        GovernanceGateKind gateKind,
        string toolName,
        string? matchedRule,
        DemoRunStatus? runStatus,
        GovernanceCodePointStatus evaluationStatus)
    {
        if (runStatus == DemoRunStatus.Allowed)
        {
            return $"{toolName} はルール「{matchedRule}」の allow 条件に合致し、Governance Gate を通過しました。";
        }

        if (runStatus == DemoRunStatus.Denied)
        {
            return gateKind switch
            {
                GovernanceGateKind.ExplicitDenyRule =>
                    $"{toolName} はルール「{matchedRule}」の deny 条件に合致したため、Governance Gate を通過できませんでした。",
                GovernanceGateKind.DefaultDeny =>
                    $"{toolName} に合致する allow ルールがないため、default_action: deny により Governance Gate を通過できませんでした。",
                GovernanceGateKind.PromptInjectionDetection =>
                    "入力から prompt injection が検出されたため、Governance Gate を通過できませんでした。",
                _ => $"{toolName} は Governance Gate を通過できませんでした。"
            };
        }

        if (runStatus == DemoRunStatus.Failed
            || evaluationStatus == GovernanceCodePointStatus.Failed)
        {
            return "Governance Gate の評価を完了できませんでした。";
        }

        if (evaluationStatus == GovernanceCodePointStatus.Active)
        {
            return $"{toolName} を定義ファイルの条件と照合しています。";
        }

        return $"{toolName} はまだ Governance Gate で評価されていません。";
    }

    private static GovernanceGateKind ExpectedGateFor(GovernanceScenario scenario) =>
        scenario.Id switch
        {
            "shell-explicitly-denied" => GovernanceGateKind.ExplicitDenyRule,
            "unknown-default-denied" => GovernanceGateKind.DefaultDeny,
            "prompt-injection-denied" => GovernanceGateKind.PromptInjectionDetection,
            _ => GovernanceGateKind.AllowlistRule
        };

    private static GovernanceCodePointStatus EvaluationStatus(
        ExecutionFlowState flow,
        DemoRunStatus? runStatus)
    {
        if (runStatus == DemoRunStatus.Failed
            || flow.GovernanceGate.Status == ExecutionFlowStageStatus.Failed)
        {
            return GovernanceCodePointStatus.Failed;
        }

        if (runStatus == DemoRunStatus.Allowed
            || flow.GovernanceGate.Status == ExecutionFlowStageStatus.Succeeded)
        {
            return GovernanceCodePointStatus.Allowed;
        }

        if (runStatus == DemoRunStatus.Denied
            || flow.GovernanceGate.Status == ExecutionFlowStageStatus.Denied)
        {
            return GovernanceCodePointStatus.Blocked;
        }

        if (runStatus == DemoRunStatus.Running
            && flow.GovernanceGate.Status == ExecutionFlowStageStatus.Active)
        {
            return GovernanceCodePointStatus.Active;
        }

        return GovernanceCodePointStatus.Pending;
    }

    private static GovernanceCodePointStatus DenyPathStatus(
        ExecutionFlowState flow,
        DemoRunStatus? runStatus,
        GovernanceCodePointStatus evaluationStatus)
    {
        if (evaluationStatus == GovernanceCodePointStatus.Allowed)
        {
            return GovernanceCodePointStatus.NotTaken;
        }

        if (evaluationStatus == GovernanceCodePointStatus.Failed)
        {
            return GovernanceCodePointStatus.Failed;
        }

        if (runStatus == DemoRunStatus.Denied
            || flow.ToolExecution.Status == ExecutionFlowStageStatus.Skipped)
        {
            return GovernanceCodePointStatus.Blocked;
        }

        if (evaluationStatus == GovernanceCodePointStatus.Blocked)
        {
            return GovernanceCodePointStatus.Active;
        }

        return GovernanceCodePointStatus.Pending;
    }

    private static string SummaryFor(
        GovernanceGateKind gateKind,
        string toolName,
        DemoRunStatus? runStatus,
        GovernanceCodePointStatus evaluationStatus)
    {
        if (runStatus == DemoRunStatus.Denied)
        {
            return $"{GateLabelFor(gateKind)}が {toolName} を拒否し、早期 return でツール実行をブロックしました。";
        }

        if (runStatus == DemoRunStatus.Allowed)
        {
            return $"{GateLabelFor(gateKind)}が {toolName} を許可したため、deny ブロックパスは通過していません。";
        }

        if (evaluationStatus == GovernanceCodePointStatus.Active)
        {
            return $"{GateLabelFor(gateKind)}で {toolName} を評価しています。";
        }

        return $"{toolName} の実行前に {GateLabelFor(gateKind)}を適用する予定です。";
    }

    private static string GateLabelFor(GovernanceGateKind gateKind) =>
        gateKind switch
        {
            GovernanceGateKind.AllowlistRule => "Allowlist ルール",
            GovernanceGateKind.ExplicitDenyRule => "明示的 deny ルール",
            GovernanceGateKind.DefaultDeny => "Default deny",
            GovernanceGateKind.PromptInjectionDetection => "Prompt injection 検出",
            _ => "ガバナンスゲート"
        };
}
