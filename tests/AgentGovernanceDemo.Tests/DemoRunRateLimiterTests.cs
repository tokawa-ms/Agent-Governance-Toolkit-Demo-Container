using AgentGovernanceDemo.Configuration;
using AgentGovernanceDemo.Integration;
using Microsoft.Extensions.Options;

namespace AgentGovernanceDemo.Tests;

public sealed class DemoRunRateLimiterTests
{
    [Fact]
    public void Enforces_limit_per_client_and_recovers_after_window()
    {
        var limiter = new DemoRunRateLimiter(Options.Create(new DemoOptions
        {
            MaxRunsPerMinute = 2
        }));
        var now = new DateTimeOffset(2026, 7, 24, 0, 0, 0, TimeSpan.Zero);

        Assert.True(limiter.TryAcquire("client-a", now, out _));
        Assert.True(limiter.TryAcquire("client-a", now.AddSeconds(1), out _));
        Assert.False(limiter.TryAcquire("client-a", now.AddSeconds(2), out var retryAfter));
        Assert.True(retryAfter > TimeSpan.Zero);
        Assert.True(limiter.TryAcquire("client-b", now.AddSeconds(2), out _));
        Assert.True(limiter.TryAcquire("client-a", now.AddMinutes(1), out _));
    }
}
