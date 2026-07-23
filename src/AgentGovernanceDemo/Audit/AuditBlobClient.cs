using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace AgentGovernanceDemo.Audit;

public interface IAuditBlobClient
{
    ValueTask AppendAsync(
        string blobName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken);

    ValueTask<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken);
}

public sealed class BlobAuditOptions
{
    public required Uri AccountUri { get; init; }

    public required string ContainerName { get; init; }
}

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
