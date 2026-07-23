using System.Text;
using System.Text.Json;
using AgentGovernance.Audit;

namespace AgentGovernanceDemo.Audit;

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

    public static string GetBlobName(DateTimeOffset timestamp) =>
        $"governance-audit-{timestamp.UtcDateTime:yyyyMMdd}.jsonl";
}
