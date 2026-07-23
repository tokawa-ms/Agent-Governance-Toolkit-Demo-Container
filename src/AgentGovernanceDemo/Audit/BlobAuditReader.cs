// EN: Restores recent session-specific audit records from the current daily JSONL blob.
// JA: 当日の JSONL Blob からセッション単位の直近監査レコードを復元します。

using System.Text;
using System.Text.Json;

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Reads, validates, filters, and bounds persisted audit records for one demo session.<br/>
/// JA: 1 つのデモセッションについて、永続化済み監査レコードを読み取り、検証、絞り込み、件数制限します。
/// </summary>
public sealed class BlobAuditReader
{
    private readonly IAuditBlobClient _blobClient;
    private readonly TimeProvider _timeProvider;

    public BlobAuditReader(IAuditBlobClient blobClient, TimeProvider? timeProvider = null)
    {
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// EN: Reads the most recent matching records while preserving their original order.<br/>
    /// JA: 元の順序を維持しながら、一致する直近レコードを読み取ります。
    /// </summary>
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
