using System.Text;
using System.Text.Json;

namespace AgentGovernanceDemo.Audit;

public sealed class BlobAuditReader
{
    private readonly IAuditBlobClient _blobClient;
    private readonly TimeProvider _timeProvider;

    public BlobAuditReader(IAuditBlobClient blobClient, TimeProvider? timeProvider = null)
    {
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<IReadOnlyList<GovernanceAuditRecord>> ReadRecentAsync(
        string sessionId,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
        cancellationToken.ThrowIfCancellationRequested();

        var blobName = BlobAuditSink.GetBlobName(_timeProvider.GetUtcNow());
        await using var stream = await _blobClient.OpenReadAsync(blobName, cancellationToken)
            .ConfigureAwait(false);
        if (stream is null)
        {
            return [];
        }

        var records = new Queue<GovernanceAuditRecord>(maxCount);
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);

        try
        {
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var record = JsonSerializer.Deserialize<GovernanceAuditRecord>(line, AuditJson.Options)
                    ?? throw new JsonException("An audit JSONL record was null.");
                if (!string.Equals(record.SessionId, sessionId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (records.Count == maxCount)
                {
                    records.Dequeue();
                }

                records.Enqueue(record);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            throw new AuditReadException($"Blob '{blobName}' contains an invalid audit record.", exception);
        }

        return records.ToArray();
    }
}
