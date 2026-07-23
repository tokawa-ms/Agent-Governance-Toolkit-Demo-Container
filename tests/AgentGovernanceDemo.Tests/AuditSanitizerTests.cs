using AgentGovernance.Audit;
using AgentGovernanceDemo.Audit;

namespace AgentGovernanceDemo.Tests;

public sealed class AuditSanitizerTests
{
    [Fact]
    public void Sanitize_MasksSensitiveKeysAndCredentialPatterns()
    {
        var governanceEvent = CreateEvent(new Dictionary<string, object>
        {
            ["password"] = "super-secret",
            ["nested"] = new Dictionary<string, object>
            {
                ["api_key"] = "abc123",
                ["message"] = "Contact owner@example.com with Bearer abc.def-123"
            },
            ["connection"] = "Server=db;User Id=admin;Password=p@ssword;"
        });

        var record = new AuditSanitizer().Sanitize(governanceEvent);
        var nested = Assert.IsType<System.Text.Json.JsonElement>(record.Data["nested"]);

        Assert.Equal(AuditSanitizer.RedactedValue, record.Data["password"]?.ToString());
        Assert.Equal(AuditSanitizer.RedactedValue, nested.GetProperty("api_key").GetString());
        Assert.Equal(
            "Contact [REDACTED_EMAIL] with Bearer [REDACTED]",
            nested.GetProperty("message").GetString());
        Assert.Equal(AuditSanitizer.RedactedValue, record.Data["connection"]?.ToString());
    }

    [Fact]
    public void Sanitize_FailsClosedForUnsupportedData()
    {
        var governanceEvent = CreateEvent(new Dictionary<string, object>
        {
            ["unsafe"] = new Action(() => { })
        });

        Assert.Throws<AuditSanitizationException>(
            () => new AuditSanitizer().Sanitize(governanceEvent));
    }

    private static GovernanceEvent CreateEvent(Dictionary<string, object> data) => new()
    {
        EventId = "event-1",
        Timestamp = new DateTimeOffset(2026, 7, 24, 0, 0, 0, TimeSpan.Zero),
        Type = GovernanceEventType.PolicyCheck,
        AgentId = "agent-1",
        SessionId = "session-1",
        PolicyName = "policy-1",
        Data = data
    };
}
