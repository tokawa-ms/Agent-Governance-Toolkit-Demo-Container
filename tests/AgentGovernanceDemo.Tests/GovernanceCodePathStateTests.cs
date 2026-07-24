// EN: Verifies explainable governance gate selection and code-path highlighting.
// JA: 説明可能なガバナンスゲート選択とコードパス強調を検証します。

using AgentGovernanceDemo.Governance;
using AgentGovernanceDemo.Presentation;

namespace AgentGovernanceDemo.Tests;

public sealed class GovernanceCodePathStateTests
{
    [Theory]
    [InlineData("weather-allowed", GovernanceGateKind.AllowlistRule)]
    [InlineData("shell-explicitly-denied", GovernanceGateKind.ExplicitDenyRule)]
    [InlineData("unknown-default-denied", GovernanceGateKind.DefaultDeny)]
    [InlineData("prompt-injection-denied", GovernanceGateKind.PromptInjectionDetection)]
    public void Before_run_uses_the_selected_scenarios_expected_gate(
        string scenarioId,
        GovernanceGateKind expectedGate)
    {
        var scenario = GovernanceScenarioCatalog.GetRequired(scenarioId);

        var state = GovernanceCodePathState.Create(
            scenario,
            ExecutionFlowState.Initial,
            null);

        Assert.Equal(expectedGate, state.GateKind);
        Assert.All(
            state.Points,
            point => Assert.Equal(GovernanceCodePointStatus.Pending, point.Status));
    }

    [Fact]
    public async Task Denied_run_highlights_the_gate_and_entire_block_path()
    {
        using var service = new GovernanceDemoService();
        var run = await new DemoRunCoordinator(service).RunAsync("shell-explicitly-denied");
        var flow = ExecutionFlowState.FromEvents([], run);

        var state = GovernanceCodePathState.Create(run.Scenario, flow, run.Status);

        Assert.Equal(GovernanceGateKind.ExplicitDenyRule, state.GateKind);
        Assert.Equal("explicitly-deny-shell", state.MatchedRule);
        Assert.All(
            state.Points,
            point => Assert.Equal(GovernanceCodePointStatus.Blocked, point.Status));
        Assert.Contains("execute_shell", state.Summary);
    }

    [Fact]
    public async Task Allowed_run_marks_the_deny_path_as_not_taken()
    {
        using var service = new GovernanceDemoService();
        var run = await new DemoRunCoordinator(service).RunAsync("weather-allowed");
        var flow = ExecutionFlowState.FromEvents([], run);

        var state = GovernanceCodePathState.Create(run.Scenario, flow, run.Status);

        Assert.All(
            state.Points.Take(2),
            point => Assert.Equal(GovernanceCodePointStatus.Allowed, point.Status));
        Assert.All(
            state.Points.Skip(2),
            point => Assert.Equal(GovernanceCodePointStatus.NotTaken, point.Status));
    }

    [Fact]
    public void Curated_snippets_include_the_security_critical_source_lines()
    {
        var state = GovernanceCodePathState.Create(
            GovernanceScenarioCatalog.GetRequired("shell-explicitly-denied"),
            ExecutionFlowState.Initial,
            null);
        var lines = state.CodeSnippets
            .SelectMany(snippet => snippet.Lines)
            .Select(line => line.Text)
            .ToArray();

        Assert.Contains("var decision = _governance.Evaluate(", lines);
        Assert.Contains("return Kernel.EvaluateToolCall(", lines);
        Assert.Contains("if (!decision.Allowed)", lines);
        Assert.Contains("        DemoRunStepStatus.Skipped,", lines);
        Assert.Contains("    return new DemoRunState(", lines);

        var repositoryRoot = FindRepositoryRoot();
        var coordinatorSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "AgentGovernanceDemo",
            "Governance",
            "DemoRunCoordinator.cs"));
        var serviceSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "AgentGovernanceDemo",
            "Governance",
            "GovernanceDemoService.cs"));

        Assert.Contains("var decision = _governance.Evaluate(", coordinatorSource);
        Assert.Contains("if (!decision.Allowed)", coordinatorSource);
        Assert.Contains("DemoRunStepStatus.Skipped,", coordinatorSource);
        Assert.Contains("return new DemoRunState(", coordinatorSource);
        Assert.Contains("return Kernel.EvaluateToolCall(", serviceSource);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AgentGovernanceDemo.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
