namespace AgentGovernanceDemo.Telemetry;

public enum TelemetryState
{
    Disabled,
    Configured,
    Degraded
}

public sealed record TelemetryStatus(
    TelemetryState State,
    string ServiceName,
    string ServiceVersion,
    string EnvironmentName,
    string Message)
{
    public bool IsConfigured => State == TelemetryState.Configured;

    public bool IsDegraded => State == TelemetryState.Degraded;
}
