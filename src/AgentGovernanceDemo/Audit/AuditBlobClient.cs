// EN: Defines the Azure Blob Storage boundary used to append and read durable audit records.
// JA: 永続的な監査レコードを追記・読み取りする Azure Blob Storage 境界を定義します。

using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace AgentGovernanceDemo.Audit;

/// <summary>
/// EN: Abstracts the append-only blob operations required by the audit pipeline.<br/>
/// JA: 監査パイプラインに必要な追記専用 Blob 操作を抽象化します。
/// </summary>
public interface IAuditBlobClient
{
    /// <summary>
    /// EN: Appends one encoded audit record to the specified blob.<br/>
    /// JA: エンコード済み監査レコードを指定 Blob へ 1 件追記します。
    /// </summary>
    ValueTask AppendAsync(
        string blobName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// EN: Opens a readable stream for an existing audit blob.<br/>
    /// JA: 既存の監査 Blob を読み取るストリームを開きます。
    /// </summary>
    ValueTask<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken);
}

/// <summary>
/// EN: Holds the storage account and container settings used by the audit blob client.<br/>
/// JA: 監査 Blob クライアントが使用するストレージアカウントとコンテナーの設定を保持します。
/// </summary>
public sealed class BlobAuditOptions
{
    public required Uri AccountUri { get; init; }

    public required string ContainerName { get; init; }
}

/// <summary>
/// EN: Implements append-only audit persistence with Azure Append Blobs and token credentials.<br/>
/// JA: Azure Append Blob とトークン資格情報を使って追記専用の監査永続化を実装します。
/// </summary>
public sealed class AzureAppendBlobAuditClient : IAuditBlobClient
{
    private readonly BlobContainerClient _containerClient;

    public AzureAppendBlobAuditClient(BlobAuditOptions options, TokenCredential credential)
        : this(CreateContainerClient(options, credential))
    {
    }

    public AzureAppendBlobAuditClient(BlobContainerClient containerClient)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
    }

    public async ValueTask AppendAsync(
        string blobName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var blobClient = _containerClient.GetAppendBlobClient(blobName);
        var createResponse = await blobClient.CreateIfNotExistsAsync(
            new AppendBlobCreateOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken).ConfigureAwait(false);

        if (createResponse is null)
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(properties.Value.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Append blob '{blobName}' has content type '{properties.Value.ContentType}', expected '{contentType}'.");
            }
        }

        using var stream = new MemoryStream(content.ToArray(), writable: false);
        await blobClient.AppendBlockAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        try
        {
            return await _containerClient.GetBlobClient(blobName)
                .OpenReadAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private static BlobContainerClient CreateContainerClient(BlobAuditOptions options, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ContainerName);

        return new BlobServiceClient(options.AccountUri, credential)
            .GetBlobContainerClient(options.ContainerName);
    }
}
