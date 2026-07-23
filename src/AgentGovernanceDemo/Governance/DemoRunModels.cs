namespace AgentGovernanceDemo.Governance;

// Note 1 (EN): These statuses summarize the final outcome of one complete demo run.
// Note 1 (JA): この状態は、1 回のデモ実行全体の最終結果を表します。
public enum DemoRunStatus
{
    Running,
    Allowed,
    Denied,
    Failed
}

// Note 2 (EN): The enum order is the canonical story shown in the UI: request, gate, tool, result.
// Note 2 (JA): enum の順序は UI で説明する正式な流れ、リクエスト、ゲート、ツール、結果と一致します。
public enum DemoRunStepKind
{
    Request,
    PolicyEvaluation,
    ToolExecution,
    Result
}

// Note 3 (EN): Step status is more detailed than run status because a denied call skips the tool.
// Note 3 (JA): 拒否時にはツールが Skipped になるため、各段階の状態は実行全体より細かく表現します。
public enum DemoRunStepStatus
{
    Completed,
    Allowed,
    Denied,
    Skipped,
    Failed
}

public sealed record DemoRunStep(
    long Sequence,
    DemoRunStepKind Kind,
    DemoRunStepStatus Status,
    string Title,
    string Detail);

// Note 4 (EN): DemoRunState is the immutable result returned to the Blazor page after orchestration.
// Note 4 (JA): DemoRunState はオーケストレーション完了後に Blazor ページへ返す不変の実行結果です。
public sealed record DemoRunState(
    string SessionId,
    long Sequence,
    GovernanceScenario Scenario,
    DemoRunStatus Status,
    IReadOnlyList<DemoRunStep> Steps,
    string? Output,
    string DecisionReason)
{
    public bool Allowed => Status == DemoRunStatus.Allowed;
}

// Note 5 (EN): GovernanceDemoEvent is the live event contract used to animate the four UI stages.
// Note 5 (JA): GovernanceDemoEvent は UI の 4 段階をリアルタイム更新するためのイベント契約です。
public sealed record GovernanceDemoEvent(
    string SessionId,
    long Sequence,
    string ScenarioId,
    DemoRunStepKind Kind,
    DemoRunStepStatus Status,
    string Message);

public interface IGovernanceDemoEventSink
{
    ValueTask PublishAsync(GovernanceDemoEvent governanceEvent, CancellationToken cancellationToken);
}

public sealed class NullGovernanceDemoEventSink : IGovernanceDemoEventSink
{
    public static NullGovernanceDemoEventSink Instance { get; } = new();

    private NullGovernanceDemoEventSink()
    {
    }

    public ValueTask PublishAsync(
        GovernanceDemoEvent governanceEvent,
        CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;
}
