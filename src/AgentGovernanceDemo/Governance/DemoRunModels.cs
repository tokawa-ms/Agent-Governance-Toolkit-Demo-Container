// EN: Defines immutable run, step, and live-event contracts shared by governance orchestration and presentation.
// JA: ガバナンスのオーケストレーションと表示で共有する不変の実行、段階、ライブイベント契約を定義します。

namespace AgentGovernanceDemo.Governance;

// Note 1 (EN): These statuses summarize the final outcome of one complete demo run.
// Note 1 (JA): この状態は、1 回のデモ実行全体の最終結果を表します。
/// <summary>
/// EN: Identifies the lifecycle or terminal outcome of a complete demo run.<br/>
/// JA: デモ実行全体のライフサイクルまたは最終結果を識別します。
/// </summary>
public enum DemoRunStatus
{
    Running,
    Allowed,
    Denied,
    Failed
}

// Note 2 (EN): The enum order is the canonical story shown in the UI: request, gate, tool, result.
// Note 2 (JA): enum の順序は UI で説明する正式な流れ、リクエスト、ゲート、ツール、結果と一致します。
/// <summary>
/// EN: Identifies one of the four ordered stages in a governed tool call.<br/>
/// JA: ガバナンス対象ツール呼び出しにおける 4 つの順序付き段階の 1 つを識別します。
/// </summary>
public enum DemoRunStepKind
{
    Request,
    PolicyEvaluation,
    ToolExecution,
    Result
}

// Note 3 (EN): Step status is more detailed than run status because a denied call skips the tool.
// Note 3 (JA): 拒否時にはツールが Skipped になるため、各段階の状態は実行全体より細かく表現します。
/// <summary>
/// EN: Identifies the outcome of an individual execution stage.<br/>
/// JA: 個々の実行段階の結果を識別します。
/// </summary>
public enum DemoRunStepStatus
{
    Completed,
    Allowed,
    Denied,
    Skipped,
    Failed
}

/// <summary>
/// EN: Captures one ordered stage in the completed run history.<br/>
/// JA: 完了した実行履歴内の 1 つの順序付き段階を記録します。
/// </summary>
public sealed record DemoRunStep(
    long Sequence,
    DemoRunStepKind Kind,
    DemoRunStepStatus Status,
    string Title,
    string Detail);

// Note 4 (EN): DemoRunState is the immutable result returned to the Blazor page after orchestration.
// Note 4 (JA): DemoRunState はオーケストレーション完了後に Blazor ページへ返す不変の実行結果です。
/// <summary>
/// EN: Represents the immutable aggregate result of one orchestrated demo run.<br/>
/// JA: オーケストレーションされた 1 回のデモ実行の不変集約結果を表します。
/// </summary>
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
/// <summary>
/// EN: Carries one live stage update from orchestration to the presentation layer.<br/>
/// JA: オーケストレーションから表示層へ 1 件のライブ段階更新を伝達します。
/// </summary>
public sealed record GovernanceDemoEvent(
    string SessionId,
    long Sequence,
    string ScenarioId,
    DemoRunStepKind Kind,
    DemoRunStepStatus Status,
    string Message);

/// <summary>
/// EN: Abstracts publication of live governance run events.<br/>
/// JA: ガバナンス実行のライブイベント配信を抽象化します。
/// </summary>
public interface IGovernanceDemoEventSink
{
    ValueTask PublishAsync(GovernanceDemoEvent governanceEvent, CancellationToken cancellationToken);
}

/// <summary>
/// EN: Implements a no-op event sink for callers that do not require live updates.<br/>
/// JA: ライブ更新を必要としない呼び出し元向けの何もしないイベントシンクを実装します。
/// </summary>
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
