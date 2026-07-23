// EN: Verifies atomic JSONL appends, concurrency serialization, and publish-after-persist ordering.
// JA: JSONL の原子的追記、同時実行の直列化、永続化後通知の順序を検証します。

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AgentGovernance.Audit;
using AgentGovernanceDemo.Audit;

namespace AgentGovernanceDemo.Tests;

/// <summary>
/// EN: Tests durability and ordering guarantees provided by <see cref="BlobAuditSink"/>.<br/>
/// JA: <see cref="BlobAuditSink"/> が提供する永続性と順序保証をテストします。
/// </summary>
public sealed class BlobAuditSinkTests
{
    [Fact]
    public async Task PersistAsync_WritesValidJsonlWithoutInterleavingUnderConcurrency()
    {
        var client = new FakeAuditBlobClient();
        using var hub = new PersistedAuditEventHub();
        await using var sink = new BlobAuditSink(client, new AuditSanitizer(), hub);

        var writes = Enumerable.Range(0, 50)
            .Select(index => sink.PersistAsync(CreateEvent(index)).AsTask());
        await Task.WhenAll(writes);

        Assert.Equal(1, client.MaximumConcurrentAppends);
        Assert.All(client.Appends, append =>
        {
            Assert.Equal("governance-audit-20260724.jsonl", append.BlobName);
            Assert.Equal(BlobAuditSink.ContentType, append.ContentType);
            Assert.EndsWith("\n", append.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("\r", append.Text, StringComparison.Ordinal);
            using var document = JsonDocument.Parse(append.Text);
            Assert.True(document.RootElement.TryGetProperty("EventId", out _));
            Assert.Equal("PolicyCheck", document.RootElement.GetProperty("Type").GetString());
        });
    }

    [Fact]
    public async Task PersistAsync_PublishesOnlyAfterSuccessfulAppend()
    {
        var client = new FakeAuditBlobClient();
        using var hub = new PersistedAuditEventHub();
        var observedAfterAppend = false;
        using var subscription = hub.Subscribe("session-1", record =>
        {
            observedAfterAppend = client.Appends.Any(append => append.Text.Contains(record.EventId));
        });
        await using var sink = new BlobAuditSink(client, new AuditSanitizer(), hub);

        await sink.PersistAsync(CreateEvent(1));

        Assert.True(observedAfterAppend);
        Assert.Single(hub.GetRecent("session-1", 10));
    }

    [Fact]
    public async Task PersistAsync_DoesNotPublishWhenAppendFails()
    {
        var client = new FakeAuditBlobClient { AppendException = new IOException("storage unavailable") };
        using var hub = new PersistedAuditEventHub();
        var publishCount = 0;
        using var subscription = hub.Subscribe("session-1", _ => publishCount++);
        await using var sink = new BlobAuditSink(client, new AuditSanitizer(), hub);

        var exception = await Assert.ThrowsAsync<AuditPersistenceException>(
            () => sink.PersistAsync(CreateEvent(1)).AsTask());

        Assert.IsType<IOException>(exception.InnerException);
        Assert.Equal(0, publishCount);
        Assert.Empty(hub.GetRecent("session-1", 10));
    }

    private static GovernanceEvent CreateEvent(int index) => new()
    {
        EventId = $"event-{index}",
        Timestamp = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
        Type = GovernanceEventType.PolicyCheck,
        AgentId = "agent-1",
        SessionId = "session-1",
        PolicyName = "policy-1",
        Data = new Dictionary<string, object> { ["index"] = index }
    };

    /// <summary>
    /// EN: Records append calls, simulates failures, and measures append concurrency.<br/>
    /// JA: 追記呼び出しを記録し、失敗を模擬して、追記の同時実行数を計測します。
    /// </summary>
    private sealed class FakeAuditBlobClient : IAuditBlobClient
    {
        private int _activeAppends;
        private int _maximumConcurrentAppends;

        public ConcurrentQueue<AppendCall> Appends { get; } = new();

        public Exception? AppendException { get; init; }

        public int MaximumConcurrentAppends => Volatile.Read(ref _maximumConcurrentAppends);

        public async ValueTask AppendAsync(
            string blobName,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _activeAppends);
            InterlockedExtensions.Max(ref _maximumConcurrentAppends, active);
            try
            {
                await Task.Delay(2, cancellationToken);
                if (AppendException is not null)
                {
                    throw AppendException;
                }

                Appends.Enqueue(new AppendCall(blobName, contentType, Encoding.UTF8.GetString(content.Span)));
            }
            finally
            {
                Interlocked.Decrement(ref _activeAppends);
            }
        }

        public ValueTask<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken) =>
            ValueTask.FromResult<Stream?>(null);
    }

    /// <summary>
    /// EN: Captures one append invocation for assertions.<br/>
    /// JA: アサーション向けに 1 回の追記呼び出しを記録します。
    /// </summary>
    private sealed record AppendCall(string BlobName, string ContentType, string Text);

    /// <summary>
    /// EN: Provides an atomic maximum operation for the concurrency probe.<br/>
    /// JA: 同時実行数の計測向けにアトミックな最大値更新を提供します。
    /// </summary>
    private static class InterlockedExtensions
    {
        public static void Max(ref int location, int value)
        {
            var current = Volatile.Read(ref location);
            while (current < value)
            {
                var observed = Interlocked.CompareExchange(ref location, value, current);
                if (observed == current)
                {
                    return;
                }

                current = observed;
            }
        }
    }
}
