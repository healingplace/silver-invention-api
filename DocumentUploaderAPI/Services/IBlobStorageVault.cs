using DocumentUploaderAPI.Models;

namespace DocumentUploaderAPI.Services;

/// <summary>
/// Vault pattern interface for abstracting blob storage operations.
/// This interface decouples the API layer from Azure Blob Storage implementation.
/// </summary>
public interface IBlobStorageVault
{
    /// <summary>
    /// Uploads a document to blob storage.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="metadata">Optional metadata to associate with the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>DocumentInfo containing upload details.</returns>
    Task<DocumentInfo> UploadDocumentAsync(IFormFile file, string? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a document from blob storage as a stream.
    /// </summary>
    /// <param name="documentId">The ID/blob name of the document to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the stream and content type.</returns>
    Task<(Stream Stream, string ContentType)> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from blob storage.
    /// </summary>
    /// <param name="documentId">The ID/blob name of the document to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully, false if document not found.</returns>
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all documents in the blob storage container.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of DocumentInfo for all blobs in the container.</returns>
    Task<IEnumerable<DocumentInfo>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a document exists in blob storage.
    /// </summary>
    /// <param name="documentId">The ID/blob name to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the document exists, false otherwise.</returns>
    Task<bool> DocumentExistsAsync(string documentId, CancellationToken cancellationToken = default);
}
