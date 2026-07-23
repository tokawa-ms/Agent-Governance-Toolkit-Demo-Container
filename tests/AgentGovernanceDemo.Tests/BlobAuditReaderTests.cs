// EN: Verifies session filtering, recent-record bounds, and daily blob selection during audit restoration.
// JA: 監査復元時のセッション絞り込み、直近件数制限、日単位 Blob 選択を検証します。

using System.Text;
using System.Text.Json;
using AgentGovernance.Audit;
using AgentGovernanceDemo.Audit;

namespace AgentGovernanceDemo.Tests;

/// <summary>
/// EN: Tests the read and filtering behavior of <see cref="BlobAuditReader"/>.<br/>
/// JA: <see cref="BlobAuditReader"/> の読み取りと絞り込み動作をテストします。
/// </summary>
public sealed class BlobAuditReaderTests
{
    [Fact]
    public async Task ReadRecentAsync_ReturnsBoundedRecentRecordsForSession()
    {
        var records = new[]
        {
            CreateRecord("one", "session-1"),
            CreateRecord("other", "session-2"),
            CreateRecord("two", "session-1"),
            CreateRecord("three", "session-1")
        };
        var jsonl = string.Join(
            "\n",
            records.Select(record => JsonSerializer.Serialize(record, AuditJsonForTests.Options))) + "\n";
        var client = new ReadOnlyAuditBlobClient(jsonl);
        var reader = new BlobAuditReader(
            client,
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 24, 20, 0, 0, TimeSpan.Zero)));

        var result = await reader.ReadRecentAsync("session-1", 2);

        Assert.Equal(["two", "three"], result.Select(record => record.EventId));
        Assert.Equal("governance-audit-20260724.jsonl", client.RequestedBlobName);
    }

    private static GovernanceAuditRecord CreateRecord(string eventId, string sessionId) => new()
    {
        EventId = eventId,
        Timestamp = new DateTimeOffset(2026, 7, 24, 0, 0, 0, TimeSpan.Zero),
        Type = GovernanceEventType.PolicyCheck,
        AgentId = "agent-1",
        SessionId = sessionId,
        Data = []
    };

    /// <summary>
    /// EN: Supplies an in-memory read stream and records the requested blob name.<br/>
    /// JA: メモリ内読み取りストリームを提供し、要求された Blob 名を記録します。
    /// </summary>
    private sealed class ReadOnlyAuditBlobClient(string content) : IAuditBlobClient
    {
        public string? RequestedBlobName { get; private set; }

        public ValueTask AppendAsync(
            string blobName,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
        {
            RequestedBlobName = blobName;
            return ValueTask.FromResult<Stream?>(
                new MemoryStream(Encoding.UTF8.GetBytes(content), writable: false));
        }
    }

    /// <summary>
    /// EN: Provides a deterministic UTC clock for daily blob-name assertions.<br/>
    /// JA: 日単位 Blob 名のアサーション向けに決定論的な UTC 時刻を提供します。
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    /// <summary>
    /// EN: Mirrors the production enum serialization needed to build test JSONL records.<br/>
    /// JA: テスト用 JSONL レコード生成に必要な本番同等の enum シリアライズを提供します。
    /// </summary>
    private static class AuditJsonForTests
    {
        public static JsonSerializerOptions Options { get; } = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            return options;
        }
    }
}
