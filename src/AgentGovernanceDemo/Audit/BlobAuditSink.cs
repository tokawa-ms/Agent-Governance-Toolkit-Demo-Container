// EN: Serializes sanitized governance events as newline-delimited JSON and appends them atomically to Blob Storage.
// JA: サニタイズ済みガバナンスイベントを改行区切り JSON に変換し、Blob Storage へ排他的に追記します。

using System.Text;
using System.Text.Json;
using AgentGovernance.Audit;

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Persists sanitized governance events as append-only JSONL audit records.<br/>
/// JA: サニタイズ済みガバナンスイベントを追記専用 JSONL 監査レコードとして永続化します。
/// </summary>
/// <remarks>
/// EN: A record is published to in-memory subscribers only after the blob append succeeds.<br/>
/// JA: Blob への追記成功後に限り、レコードをメモリ内購読者へ通知します。
/// </remarks>
public sealed class BlobAuditSink : IAsyncDisposable
{
    public const string ContentType = "application/x-ndjson";
    private const int MaximumAppendBlockSize = 4 * 1024 * 1024;
    private static readonly SemaphoreSlim ProcessAppendLock = new(1, 1);

    private readonly IAuditBlobClient _blobClient;
    private readonly IAuditSanitizer _sanitizer;
    private readonly PersistedAuditEventHub _eventHub;
    private int _disposed;

    public BlobAuditSink(
        IAuditBlobClient blobClient,
        IAuditSanitizer sanitizer,
        PersistedAuditEventHub eventHub)
    {
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        _eventHub = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
    }

    /// <summary>
    /// EN: Sanitizes, serializes, appends, and publishes one governance event.<br/>
    /// JA: 1 件のガバナンスイベントをサニタイズ、シリアライズ、追記、通知します。
    /// </summary>
    public async ValueTask<GovernanceAuditRecord> PersistAsync(
        GovernanceEvent governanceEvent,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested();

        var record = _sanitizer.Sanitize(governanceEvent);
        byte[] line;
        try
        {
            var json = JsonSerializer.Serialize(record, AuditJson.Options);
            line = Encoding.UTF8.GetBytes(json + "\n");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new AuditPersistenceException(
                $"Audit event '{record.EventId}' could not be serialized.",
                exception);
        }

        if (line.Length > MaximumAppendBlockSize)
        {
            throw new InvalidOperationException(
                $"Audit event '{record.EventId}' exceeds the append blob block limit.");
        }

        await ProcessAppendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            await _blobClient.AppendAsync(
                GetBlobName(record.Timestamp),
                line,
                ContentType,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ObjectDisposedException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AuditPersistenceException(
                $"Failed to append audit event '{record.EventId}'.",
                exception);
        }
        finally
        {
            ProcessAppendLock.Release();
        }

        _eventHub.Publish(record);
        return record;
    }

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// EN: Produces the UTC daily append-blob name for a timestamp.<br/>
    /// JA: タイムスタンプから UTC 日単位の Append Blob 名を生成します。
    /// </summary>
    public static string GetBlobName(DateTimeOffset timestamp) =>
        $"governance-audit-{timestamp.UtcDateTime:yyyyMMdd}.jsonl";
}
