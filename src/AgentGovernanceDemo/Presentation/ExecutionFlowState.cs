// EN: Projects domain run events into the four-stage immutable view model rendered by the Blazor UI.
// JA: ドメイン実行イベントを Blazor UI が描画する 4 段階の不変ビューモデルへ変換します。

using AgentGovernanceDemo.Governance;

namespace AgentGovernanceDemo.Presentation;

/// <summary>
/// EN: Identifies one of the four customer-facing execution stages.<br/>
/// JA: お客様向けに表示する 4 つの実行段階の 1 つを識別します。
/// </summary>
public enum ExecutionFlowStageKind
{
    Request,
    GovernanceGate,
    ToolExecution,
    Result
}

/// <summary>
/// EN: Identifies the visual state of an execution-flow stage.<br/>
/// JA: 実行フロー段階の表示状態を識別します。
/// </summary>
public enum ExecutionFlowStageStatus
{
    Pending,
    Active,
    Succeeded,
    Denied,
    Skipped,
    Failed
}

/// <summary>
/// EN: Represents one immutable card in the execution-flow visualization.<br/>
/// JA: 実行フロー可視化内の 1 枚の不変カードを表します。
/// </summary>
public sealed record ExecutionFlowStage(
    ExecutionFlowStageKind Kind,
    string Title,
    ExecutionFlowStageStatus Status,
    string? Message);

/// <summary>
/// EN: Reconciles live events and final run results into a consistent four-stage UI state.<br/>
/// JA: ライブイベントと最終実行結果を、一貫した 4 段階の UI 状態へ統合します。
/// </summary>
public sealed class ExecutionFlowState
{
    // Note 1 (EN): StageOrder is the presentation contract shared with the four-step customer narrative.
    // Note 1 (JA): StageOrder は、お客様へ説明する 4 段階と共有する表示上の契約です。
    // Note 1 (EN): Keep this order aligned with the strongly typed stage accessors below.
    // Note 1 (JA): 下にある型付き stage accessor と同じ順序を維持する必要があります。
    private static readonly ExecutionFlowStageKind[] StageOrder =
    [
        ExecutionFlowStageKind.Request,
        ExecutionFlowStageKind.GovernanceGate,
        ExecutionFlowStageKind.ToolExecution,
        ExecutionFlowStageKind.Result
    ];

    private readonly IReadOnlyList<ExecutionFlowStage> _stages;

    private ExecutionFlowState(
        IReadOnlyList<ExecutionFlowStage> stages,
        string? decisionReason,
        string? output)
    {
        _stages = stages;
        DecisionReason = decisionReason;
        Output = output;
    }

    public IReadOnlyList<ExecutionFlowStage> Stages => _stages;

    public ExecutionFlowStage Request => _stages[0];

    public ExecutionFlowStage GovernanceGate => _stages[1];

    public ExecutionFlowStage ToolExecution => _stages[2];

    public ExecutionFlowStage Result => _stages[3];

    public string? DecisionReason { get; }

    public string? Output { get; }

    public static ExecutionFlowState Initial { get; } = FromEvents([]);

    public static ExecutionFlowState FromEvents(
        IEnumerable<GovernanceDemoEvent> events,
        DemoRunState? runState = null) =>
        Create(events, runState, null);

    public static ExecutionFlowState Failed(
        IEnumerable<GovernanceDemoEvent> events,
        ExecutionFlowStageKind stage,
        string message,
        DemoRunState? runState = null) =>
        Create(
            events,
            runState,
            new FlowTermination(stage, $"Failed: {message}", ExecutionFlowStageStatus.Failed));

    public static ExecutionFlowState Cancelled(
        IEnumerable<GovernanceDemoEvent> events,
        ExecutionFlowStageKind stage,
        string message,
        DemoRunState? runState = null) =>
        Create(
            events,
            runState,
            new FlowTermination(stage, $"Cancelled: {message}", ExecutionFlowStageStatus.Failed));

    private static ExecutionFlowState Create(
        IEnumerable<GovernanceDemoEvent> events,
        DemoRunState? runState,
        FlowTermination? termination)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Note 2 (EN): Multiple updates can exist for one stage, so the highest sequence is authoritative.
        // Note 2 (JA): 同じ段階に複数更新があるため、最大 sequence のイベントを正として採用します。
        var latestEvents = events
            .GroupBy(governanceEvent => ToStageKind(governanceEvent.Kind))
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(governanceEvent => governanceEvent.Sequence).First());

        var stages = StageOrder
            .Select(kind => CreateStage(kind, latestEvents.GetValueOrDefault(kind)))
            .ToArray();

        // Note 3 (EN): Decision reason belongs to the gate; output belongs only to a successful result.
        // Note 3 (JA): 判断理由はゲートに属し、output は成功した結果にだけ属します。
        // Note 3 (EN): Keeping them separate makes denied runs explainable without inventing tool output.
        // Note 3 (JA): 両者を分離することで、拒否時に架空のツール出力を作らず理由だけを説明できます。
        var decisionReason = runState?.DecisionReason
            ?? latestEvents.GetValueOrDefault(ExecutionFlowStageKind.GovernanceGate)?.Message;
        var resultEvent = latestEvents.GetValueOrDefault(ExecutionFlowStageKind.Result);
        var output = runState?.Output
            ?? (resultEvent?.Status is DemoRunStepStatus.Completed or DemoRunStepStatus.Allowed
                ? resultEvent.Message
                : null);

        if (termination is not null)
        {
            // Note 4 (EN): Cancellation or unexpected failure terminates the current stage and skips later ones.
            // Note 4 (JA): キャンセルや予期しない失敗では、現在段階を終了し、後続段階を Skipped にします。
            ApplyTermination(stages, termination);
        }
        else if (runState?.Status == DemoRunStatus.Allowed)
        {
            // Note 5 (EN): A completed allowed run guarantees all four stages succeeded.
            // Note 5 (JA): 許可された実行が完了した場合、4 段階すべてを Succeeded として確定します。
            SetAllSucceeded(stages);
        }
        else if (runState?.Status == DemoRunStatus.Denied
            || stages[(int)ExecutionFlowStageKind.GovernanceGate].Status == ExecutionFlowStageStatus.Denied)
        {
            // Note 6 (EN): A denied gate forces Tool Execution to Skipped and Result to Denied.
            // Note 6 (JA): ゲートが拒否した場合、Tool Execution は Skipped、Result は Denied になります。
            ApplyDenied(stages, decisionReason);
        }
        else if (runState?.Status == DemoRunStatus.Failed)
        {
            ApplyRunFailure(stages);
        }
        else if (!HasTerminalStatus(stages))
        {
            ActivateNextStage(stages);
        }

        return new ExecutionFlowState(Array.AsReadOnly(stages), decisionReason, output);
    }

    private static ExecutionFlowStage CreateStage(
        ExecutionFlowStageKind kind,
        GovernanceDemoEvent? governanceEvent) =>
        new(
            kind,
            GetTitle(kind),
            governanceEvent is null
                ? ExecutionFlowStageStatus.Pending
                : ToDisplayStatus(governanceEvent.Status),
            governanceEvent?.Message);

    private static void ApplyDenied(ExecutionFlowStage[] stages, string? decisionReason)
    {
        // Note 7 (EN): This projection mirrors the coordinator's fail-closed early-return path.
        // Note 7 (JA): この表示変換は、コーディネーターの fail-closed な早期 return と対応します。
        stages[(int)ExecutionFlowStageKind.Request] =
            SetStatus(stages[(int)ExecutionFlowStageKind.Request], ExecutionFlowStageStatus.Succeeded);
        stages[(int)ExecutionFlowStageKind.GovernanceGate] =
            SetStatus(
                stages[(int)ExecutionFlowStageKind.GovernanceGate],
                ExecutionFlowStageStatus.Denied,
                decisionReason);
        stages[(int)ExecutionFlowStageKind.ToolExecution] =
            SetStatus(
                stages[(int)ExecutionFlowStageKind.ToolExecution],
                ExecutionFlowStageStatus.Skipped,
                stages[(int)ExecutionFlowStageKind.ToolExecution].Message ?? "Skipped because governance denied the request.");
        stages[(int)ExecutionFlowStageKind.Result] =
            SetStatus(
                stages[(int)ExecutionFlowStageKind.Result],
                ExecutionFlowStageStatus.Denied,
                stages[(int)ExecutionFlowStageKind.Result].Message ?? "Blocked by governance.");
    }

    private static void ApplyRunFailure(ExecutionFlowStage[] stages)
    {
        var failedIndex = Array.FindIndex(
            stages,
            stage => stage.Status == ExecutionFlowStageStatus.Failed);
        var failureMessage = failedIndex >= 0
            ? stages[failedIndex].Message ?? "Run failed."
            : "Run failed.";

        if (failedIndex == (int)ExecutionFlowStageKind.Result
            && stages[(int)ExecutionFlowStageKind.ToolExecution].Status
                is ExecutionFlowStageStatus.Pending or ExecutionFlowStageStatus.Active)
        {
            stages[(int)ExecutionFlowStageKind.ToolExecution] =
                SetStatus(
                    stages[(int)ExecutionFlowStageKind.ToolExecution],
                    ExecutionFlowStageStatus.Failed,
                    failureMessage);
        }

        if (failedIndex < 0)
        {
            var firstIncomplete = Array.FindIndex(
                stages,
                stage => stage.Status is ExecutionFlowStageStatus.Pending or ExecutionFlowStageStatus.Active);
            ApplyTermination(
                stages,
                new FlowTermination(
                    firstIncomplete < 0 ? ExecutionFlowStageKind.Result : (ExecutionFlowStageKind)firstIncomplete,
                    failureMessage,
                    ExecutionFlowStageStatus.Failed));
        }
    }

    private static void ApplyTermination(ExecutionFlowStage[] stages, FlowTermination termination)
    {
        var terminalIndex = (int)termination.Stage;
        stages[terminalIndex] =
            SetStatus(stages[terminalIndex], termination.Status, termination.Message);

        for (var index = terminalIndex + 1; index < stages.Length; index++)
        {
            stages[index] = SetStatus(
                stages[index],
                ExecutionFlowStageStatus.Skipped,
                stages[index].Message ?? termination.Message);
        }
    }

    private static void SetAllSucceeded(ExecutionFlowStage[] stages)
    {
        for (var index = 0; index < stages.Length; index++)
        {
            stages[index] = SetStatus(stages[index], ExecutionFlowStageStatus.Succeeded);
        }
    }

    private static void ActivateNextStage(ExecutionFlowStage[] stages)
    {
        var nextIndex = Array.FindIndex(
            stages,
            stage => stage.Status == ExecutionFlowStageStatus.Pending);

        if (nextIndex >= 0)
        {
            stages[nextIndex] = SetStatus(stages[nextIndex], ExecutionFlowStageStatus.Active);
        }
    }

    private static bool HasTerminalStatus(IEnumerable<ExecutionFlowStage> stages) =>
        stages.Any(stage => stage.Status
            is ExecutionFlowStageStatus.Denied or ExecutionFlowStageStatus.Failed);

    private static ExecutionFlowStage SetStatus(
        ExecutionFlowStage stage,
        ExecutionFlowStageStatus status,
        string? message = null) =>
        stage with
        {
            Status = status,
            Message = message ?? stage.Message
        };

    private static ExecutionFlowStageKind ToStageKind(DemoRunStepKind kind) =>
        kind switch
        {
            // Note 8 (EN): PolicyEvaluation is intentionally labeled Governance Gate for the customer UI.
            // Note 8 (JA): PolicyEvaluation は、お客様向け UI では Governance Gate として表示します。
            DemoRunStepKind.Request => ExecutionFlowStageKind.Request,
            DemoRunStepKind.PolicyEvaluation => ExecutionFlowStageKind.GovernanceGate,
            DemoRunStepKind.ToolExecution => ExecutionFlowStageKind.ToolExecution,
            DemoRunStepKind.Result => ExecutionFlowStageKind.Result,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown demo run step kind.")
        };

    private static ExecutionFlowStageStatus ToDisplayStatus(DemoRunStepStatus status) =>
        status switch
        {
            DemoRunStepStatus.Completed or DemoRunStepStatus.Allowed =>
                ExecutionFlowStageStatus.Succeeded,
            DemoRunStepStatus.Denied => ExecutionFlowStageStatus.Denied,
            DemoRunStepStatus.Skipped => ExecutionFlowStageStatus.Skipped,
            DemoRunStepStatus.Failed => ExecutionFlowStageStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown demo run step status.")
        };

    private static string GetTitle(ExecutionFlowStageKind kind) =>
        kind switch
        {
            ExecutionFlowStageKind.Request => "Request",
            ExecutionFlowStageKind.GovernanceGate => "Governance Gate",
            ExecutionFlowStageKind.ToolExecution => "Tool Execution",
            ExecutionFlowStageKind.Result => "Result",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown execution flow stage.")
        };

    /// <summary>
    /// EN: Describes an explicit failure or cancellation point used to terminate the visual flow.<br/>
    /// JA: 表示フローを終了させる明示的な失敗またはキャンセル地点を表します。
    /// </summary>
    private sealed record FlowTermination(
        ExecutionFlowStageKind Stage,
        string Message,
        ExecutionFlowStageStatus Status);
}
