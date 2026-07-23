using AgentGovernanceDemo.Governance;

namespace AgentGovernanceDemo.Tests;

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
