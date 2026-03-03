namespace DocumentUploaderAPI.Models;

/// <summary>
/// Represents metadata information about a stored document.
/// </summary>
public class DocumentInfo
{
    /// <summary>
    /// Unique identifier for the document (blob name).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original name of the uploaded document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Size of the document in bytes.
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Content type/MIME type of the document.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the document was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// URI/URL to access the document.
    /// </summary>
    public Uri? Uri { get; set; }
}

/// <summary>
/// Request model for uploading a document.
/// </summary>
public class UploadDocumentRequest
{
    /// <summary>
    /// The document file to upload.
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Optional metadata or tags associated with the document.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Response model for document operations.
/// </summary>
public class DocumentResponse
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The document data (if applicable).
    /// </summary>
    public DocumentInfo? Data { get; set; }
}
