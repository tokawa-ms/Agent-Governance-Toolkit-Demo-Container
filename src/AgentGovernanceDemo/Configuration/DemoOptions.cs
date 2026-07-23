using System.ComponentModel.DataAnnotations;

namespace AgentGovernanceDemo.Configuration;

public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    [Range(1, 20)]
    public int MaxRunsPerMinute { get; init; } = 8;

    [Range(0, 5000)]
    public int StepDelayMilliseconds { get; init; } = 450;

    public string EnvironmentName { get; init; } = "Local";
}
