// EN: Verifies allow and deny orchestration, no-execution guarantees, and monotonic event sequencing.
// JA: 許可・拒否のオーケストレーション、未実行保証、イベント連番の単調増加を検証します。

using AgentGovernanceDemo.Governance;

namespace AgentGovernanceDemo.Tests;

/// <summary>
/// EN: Tests the security-critical stage ordering implemented by <see cref="DemoRunCoordinator"/>.<br/>
/// JA: <see cref="DemoRunCoordinator"/> が実装するセキュリティ上重要な段階順序をテストします。
/// </summary>
public sealed class DemoRunCoordinatorTests
{
    [Theory]
    [InlineData("weather-allowed")]
    [InlineData("time-allowed")]
    [InlineData("location-allowed")]
    public async Task Allowed_scenarios_execute_deterministic_tools(string scenarioId)
    {
        using var service = new GovernanceDemoService();
        var coordinator = new DemoRunCoordinator(service);

        var run = await coordinator.RunAsync(scenarioId);

        Assert.True(run.Allowed);
        Assert.NotNull(run.Output);
        Assert.NotEmpty(run.SessionId);
        Assert.True(run.Sequence > 0);
        Assert.Equal(GovernanceGateKind.AllowlistRule, run.Decision.GateKind);
        Assert.True(run.Decision.Allowed);
        Assert.Contains(run.Steps, s => s.Kind == DemoRunStepKind.ToolExecution
            && s.Status == DemoRunStepStatus.Completed);
    }

    [Theory]
    [InlineData("shell-explicitly-denied")]
    [InlineData("unknown-default-denied")]
    [InlineData("prompt-injection-denied")]
    public async Task Denied_scenarios_never_execute_tools(string scenarioId)
    {
        using var service = new GovernanceDemoService();
        var executor = new RecordingExecutor();
        var coordinator = new DemoRunCoordinator(service, executor);

        var run = await coordinator.RunAsync(scenarioId);

        Assert.Equal(DemoRunStatus.Denied, run.Status);
        Assert.False(run.Decision.Allowed);
        Assert.Equal(0, executor.ExecutionCount);
        Assert.Contains(run.Steps, s => s.Kind == DemoRunStepKind.ToolExecution
            && s.Status == DemoRunStepStatus.Skipped);
    }

    [Fact]
    public async Task Runs_and_events_have_unique_monotonic_sequences()
    {
        using var service = new GovernanceDemoService();
        var sink = new RecordingEventSink();
        var coordinator = new DemoRunCoordinator(service, events: sink);

        var first = await coordinator.RunAsync("weather-allowed");
        var second = await coordinator.RunAsync("time-allowed");

        Assert.NotEqual(first.SessionId, second.SessionId);
        Assert.True(second.Sequence > first.Sequence);
        Assert.Equal(
            sink.Events.Select(e => e.Sequence).OrderBy(sequence => sequence),
            sink.Events.Select(e => e.Sequence));
        Assert.Equal(sink.Events.Count, sink.Events.Select(e => e.Sequence).Distinct().Count());
    }

    /// <summary>
    /// EN: Counts tool invocations to prove denied scenarios never cross the execution boundary.<br/>
    /// JA: ツール呼び出し回数を数え、拒否シナリオが実行境界を越えないことを証明します。
    /// </summary>
    private sealed class RecordingExecutor : IDemoToolExecutor
    {
        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(
            string toolName,
            IReadOnlyDictionary<string, object> arguments,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return ValueTask.FromResult("executed");
        }
    }

    /// <summary>
    /// EN: Collects live events for ordering and uniqueness assertions.<br/>
    /// JA: 順序と一意性のアサーション向けにライブイベントを収集します。
    /// </summary>
    private sealed class RecordingEventSink : IGovernanceDemoEventSink
    {
        public List<GovernanceDemoEvent> Events { get; } = [];

        public ValueTask PublishAsync(
            GovernanceDemoEvent governanceEvent,
            CancellationToken cancellationToken)
        {
            Events.Add(governanceEvent);
            return ValueTask.CompletedTask;
        }
    }
}
