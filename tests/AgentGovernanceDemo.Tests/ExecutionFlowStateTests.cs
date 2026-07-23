// EN: Verifies projection of live and terminal domain events into the four-stage presentation model.
// JA: ライブおよび終端ドメインイベントから 4 段階表示モデルへの変換を検証します。

using AgentGovernanceDemo.Governance;
using AgentGovernanceDemo.Presentation;

namespace AgentGovernanceDemo.Tests;

/// <summary>
/// EN: Tests stage activation, denial, failure, cancellation, and event reconciliation rules.<br/>
/// JA: 段階の有効化、拒否、失敗、キャンセル、イベント統合ルールをテストします。
/// </summary>
public sealed class ExecutionFlowStateTests
{
    [Fact]
    public void Initial_NoEvents_ActivatesRequestAndLeavesRemainingStagesPending()
    {
        var flow = ExecutionFlowState.Initial;

        Assert.Equal(
            [
                ExecutionFlowStageStatus.Active,
                ExecutionFlowStageStatus.Pending,
                ExecutionFlowStageStatus.Pending,
                ExecutionFlowStageStatus.Pending
            ],
            flow.Stages.Select(stage => stage.Status));
        Assert.Equal(
            ["Request", "Governance Gate", "Tool Execution", "Result"],
            flow.Stages.Select(stage => stage.Title));
        Assert.Null(flow.DecisionReason);
        Assert.Null(flow.Output);
    }

    [Fact]
    public void FromEvents_ProgressiveAllow_ActivatesTheNextIncompleteStage()
    {
        var events = new[]
        {
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "request accepted"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Allowed, "policy allowed")
        };

        var flow = ExecutionFlowState.FromEvents(events);

        Assert.Equal(ExecutionFlowStageStatus.Succeeded, flow.Request.Status);
        Assert.Equal(ExecutionFlowStageStatus.Succeeded, flow.GovernanceGate.Status);
        Assert.Equal(ExecutionFlowStageStatus.Active, flow.ToolExecution.Status);
        Assert.Equal(ExecutionFlowStageStatus.Pending, flow.Result.Status);
        Assert.Equal("policy allowed", flow.DecisionReason);
    }

    [Fact]
    public void FromEvents_ToolCompletes_ActivatesResult()
    {
        var flow = ExecutionFlowState.FromEvents(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Allowed, "allowed"),
            Event(3, DemoRunStepKind.ToolExecution, DemoRunStepStatus.Completed, "tool output")
        ]);

        Assert.Equal(ExecutionFlowStageStatus.Succeeded, flow.ToolExecution.Status);
        Assert.Equal(ExecutionFlowStageStatus.Active, flow.Result.Status);
    }

    [Fact]
    public void FromEvents_AllowedRunState_MarksEveryStageSuccessfulAndExposesDetails()
    {
        var runState = RunState(
            DemoRunStatus.Allowed,
            output: "sunny",
            decisionReason: "read-only tool");

        var flow = ExecutionFlowState.FromEvents(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Allowed, "allowed")
        ],
            runState);

        Assert.All(
            flow.Stages,
            stage => Assert.Equal(ExecutionFlowStageStatus.Succeeded, stage.Status));
        Assert.Equal("read-only tool", flow.DecisionReason);
        Assert.Equal("sunny", flow.Output);
    }

    [Fact]
    public void FromEvents_ExplicitDeny_ShowsDeniedSkippedAndBlockedStages()
    {
        var flow = ExecutionFlowState.FromEvents(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Denied, "explicit deny"),
            Event(3, DemoRunStepKind.ToolExecution, DemoRunStepStatus.Skipped, "never invoked")
        ]);

        Assert.Equal(ExecutionFlowStageStatus.Succeeded, flow.Request.Status);
        Assert.Equal(ExecutionFlowStageStatus.Denied, flow.GovernanceGate.Status);
        Assert.Equal("explicit deny", flow.GovernanceGate.Message);
        Assert.Equal(ExecutionFlowStageStatus.Skipped, flow.ToolExecution.Status);
        Assert.Equal("never invoked", flow.ToolExecution.Message);
        Assert.Equal(ExecutionFlowStageStatus.Denied, flow.Result.Status);
        Assert.Equal("Blocked by governance.", flow.Result.Message);
        Assert.Equal("explicit deny", flow.DecisionReason);
    }

    [Theory]
    [InlineData("default deny")]
    [InlineData("prompt injection detected")]
    public void FromEvents_AllGovernanceDenialsUseEquivalentFlowStatuses(string reason)
    {
        var flow = ExecutionFlowState.FromEvents(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Denied, reason)
        ]);

        Assert.Equal(
            [
                ExecutionFlowStageStatus.Succeeded,
                ExecutionFlowStageStatus.Denied,
                ExecutionFlowStageStatus.Skipped,
                ExecutionFlowStageStatus.Denied
            ],
            flow.Stages.Select(stage => stage.Status));
        Assert.Equal(reason, flow.DecisionReason);
    }

    [Fact]
    public void Failed_ExplicitStage_MarksStageFailedAndLaterStagesSkipped()
    {
        var flow = ExecutionFlowState.Failed(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Allowed, "allowed")
        ],
            ExecutionFlowStageKind.ToolExecution,
            "executor unavailable");

        Assert.Equal(ExecutionFlowStageStatus.Failed, flow.ToolExecution.Status);
        Assert.Equal("Failed: executor unavailable", flow.ToolExecution.Message);
        Assert.Equal(ExecutionFlowStageStatus.Skipped, flow.Result.Status);
    }

    [Fact]
    public void Cancelled_ExplicitStage_RepresentsCancellationWithoutDomainChanges()
    {
        var flow = ExecutionFlowState.Cancelled(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested")
        ],
            ExecutionFlowStageKind.GovernanceGate,
            "user stopped the run");

        Assert.Equal(ExecutionFlowStageStatus.Failed, flow.GovernanceGate.Status);
        Assert.Equal("Cancelled: user stopped the run", flow.GovernanceGate.Message);
        Assert.Equal(ExecutionFlowStageStatus.Skipped, flow.ToolExecution.Status);
        Assert.Equal(ExecutionFlowStageStatus.Skipped, flow.Result.Status);
    }

    [Fact]
    public void FromEvents_FailedRunState_RepresentsToolAndResultFailure()
    {
        var runState = RunState(DemoRunStatus.Failed, decisionReason: "allowed");
        var flow = ExecutionFlowState.FromEvents(
        [
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "requested"),
            Event(2, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Allowed, "allowed"),
            Event(3, DemoRunStepKind.Result, DemoRunStepStatus.Failed, "tool crashed")
        ],
            runState);

        Assert.Equal(ExecutionFlowStageStatus.Failed, flow.ToolExecution.Status);
        Assert.Equal("tool crashed", flow.ToolExecution.Message);
        Assert.Equal(ExecutionFlowStageStatus.Failed, flow.Result.Status);
    }

    [Fact]
    public void FromEvents_ReorderedAndReplacementEvents_PreservesLatestMessagePerStage()
    {
        var events = new[]
        {
            Event(8, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Allowed, "latest decision"),
            Event(3, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "latest request"),
            Event(1, DemoRunStepKind.Request, DemoRunStepStatus.Completed, "stale request"),
            Event(7, DemoRunStepKind.PolicyEvaluation, DemoRunStepStatus.Denied, "stale decision")
        };

        var flow = ExecutionFlowState.FromEvents(events);

        Assert.Equal("latest request", flow.Request.Message);
        Assert.Equal("latest decision", flow.GovernanceGate.Message);
        Assert.Equal(ExecutionFlowStageStatus.Succeeded, flow.GovernanceGate.Status);
        Assert.Equal(ExecutionFlowStageStatus.Active, flow.ToolExecution.Status);
    }

    private static GovernanceDemoEvent Event(
        long sequence,
        DemoRunStepKind kind,
        DemoRunStepStatus status,
        string message) =>
        new("session", sequence, "scenario", kind, status, message);

    private static DemoRunState RunState(
        DemoRunStatus status,
        string? output = null,
        string decisionReason = "") =>
        new(
            "session",
            1,
            GovernanceScenarioCatalog.GetRequired("weather-allowed"),
            status,
            [],
            output,
            decisionReason);
}
