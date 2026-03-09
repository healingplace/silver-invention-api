using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocumentUploaderAPI.Models;

namespace DocumentUploaderAPI.Services;

/// <summary>
/// Concrete implementation of the Vault pattern for Azure Blob Storage.
/// Handles all interactions with Azure Blob Storage for document management.
/// </summary>
public class BlobStorageVault : IBlobStorageVault
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageVault> _logger;

    public BlobStorageVault(BlobContainerClient containerClient, ILogger<BlobStorageVault> logger)
    {
        _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DocumentInfo> UploadDocumentAsync(IFormFile file, string? metadata = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null.", nameof(file));
        }

        try
        {
            // Generate a unique blob name
            string blobName = $"{Guid.NewGuid()}_{file.FileName}";
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);

            // Upload the blob with metadata
            BlobUploadOptions options = new BlobUploadOptions();
            if (!string.IsNullOrEmpty(metadata))
            {
                options.Metadata = new Dictionary<string, string>
                {
                    { "metadata", metadata },
                    { "originalFileName", file.FileName }
                };
            }
            else
            {
                options.Metadata = new Dictionary<string, string>
                {
                    { "originalFileName", file.FileName }
                };
            }

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, options, cancellationToken);
            }

            // Retrieve the blob properties
            BlobProperties properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Document uploaded successfully: {BlobName}", blobName);

            return new DocumentInfo
            {
                Id = blobName,
                FileName = file.FileName,
                SizeInBytes = file.Length,
                ContentType = file.ContentType ?? "application/octet-stream",
                UploadedAt = properties.CreatedOn.DateTime,
                Uri = blobClient.Uri
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document: {FileName}", file.FileName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType)> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document ID cannot be empty.", nameof(documentId));
        }

        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(documentId);
            
            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Document '{documentId}' not found.");
            }

            // Get blob properties for content type
            BlobProperties properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            string contentType = properties.ContentType ?? "application/octet-stream";

            // Download the blob
            BlobDownloadInfo download = await blobClient.DownloadAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Document retrieved successfully: {DocumentId}", documentId);

            return (download.Content, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document: {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document ID cannot be empty.", nameof(documentId));
        }

        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(documentId);
            
            // Delete the blob and check if it existed
            Azure.Response<bool> response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogInformation("Document deleted successfully: {DocumentId}", documentId);
                return true;
            }

            _logger.LogWarning("Document not found for deletion: {DocumentId}", documentId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentInfo>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = new List<DocumentInfo>();

            // Ensure container exists first
            if (!await _containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Container does not exist yet. Creating it.");
                await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                return documents; // Return empty list for new container
            }

            // List all blobs in the container
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync(traits: Azure.Storage.Blobs.Models.BlobTraits.Metadata, cancellationToken: cancellationToken))
            {
                BlobClient blobClient = _containerClient.GetBlobClient(blobItem.Name);

                string originalFileName = blobItem.Name;
                if (blobItem.Metadata != null && blobItem.Metadata.TryGetValue("originalFileName", out var metadataFileName))
                {
                    originalFileName = metadataFileName;
                }

                documents.Add(new DocumentInfo
                {
                    Id = blobItem.Name,
                    FileName = originalFileName,
                    SizeInBytes = blobItem.Properties.ContentLength ?? 0,
                    ContentType = blobItem.Properties.ContentType ?? "application/octet-stream",
                    UploadedAt = blobItem.Properties.CreatedOn?.DateTime ?? DateTime.UtcNow,
                    Uri = blobClient.Uri
                });
            }

            _logger.LogInformation("Listed {Count} documents", documents.Count);
            return documents;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to compare two elements"))
        {
            // Known Azurite compatibility issue with .NET 10 SDK
            _logger.LogWarning("Azurite compatibility issue detected. This is a known issue with .NET 10 SDK and Azurite. Returning empty list.");
            return new List<DocumentInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DocumentExistsAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return false;
        }

        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(documentId);
            return await blobClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document exists: {DocumentId}", documentId);
            return false;
        }
    }
}
