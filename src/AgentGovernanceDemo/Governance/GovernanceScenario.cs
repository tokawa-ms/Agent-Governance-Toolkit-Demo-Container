namespace AgentGovernanceDemo.Governance;

public enum GovernanceExpectedOutcome
{
    Allow,
    Deny
}

public sealed record GovernanceScenario(
    string Id,
    string Title,
    string Description,
    string ToolName,
    IReadOnlyDictionary<string, object> Arguments,
    GovernanceExpectedOutcome ExpectedOutcome);
