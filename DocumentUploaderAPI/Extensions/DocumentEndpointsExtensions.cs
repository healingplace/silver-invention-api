using DocumentUploaderAPI.Models;
using DocumentUploaderAPI.Services;

namespace DocumentUploaderAPI.Extensions;

public static class DocumentEndpointsExtensions
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Ok(new
        {
            service = "DocumentUploaderAPI",
            status = "healthy",
            endpoints = new[]
            {
                "/health",
                "/api/documents",
                "/api/documents/upload"
            }
        }))
        .WithName("Root");

        endpoints.MapPost("/api/documents/upload", async (
            IFormFileCollection files,
            IBlobStorageVault vault,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            if (files.Count == 0)
            {
                return Results.BadRequest(new DocumentResponse
                {
                    Success = false,
                    Message = "No files provided"
                });
            }

            try
            {
                var uploadedDocuments = new List<DocumentInfo>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var documentInfo = await vault.UploadDocumentAsync(file, null, ct);
                        uploadedDocuments.Add(documentInfo);
                    }
                }

                return Results.Created(
                    "/api/documents",
                    new
                    {
                        Success = true,
                        Message = $"Successfully uploaded {uploadedDocuments.Count} document(s)",
                        Data = uploadedDocuments
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading documents");
                return Results.StatusCode(500);
            }
        })
        .WithName("UploadDocuments")
        .DisableAntiforgery();
    
        endpoints.MapGet("/api/documents/{documentId}", async (
            string documentId,
            IBlobStorageVault vault,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var (stream, contentType) = await vault.GetDocumentAsync(documentId, ct);
                return Results.File(stream, contentType, documentId);
            }
            catch (FileNotFoundException ex)
            {
                logger.LogWarning(ex, "Document not found: {DocumentId}", documentId);
                return Results.NotFound(new DocumentResponse
                {
                    Success = false,
                    Message = $"Document '{documentId}' not found"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving document: {DocumentId}", documentId);
                return Results.StatusCode(500);
            }
        })
        .WithName("GetDocument");

        endpoints.MapDelete("/api/documents/{documentId}", async (
            string documentId,
            IBlobStorageVault vault,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await vault.DeleteDocumentAsync(documentId, ct);

                if (!deleted)
                {
                    return Results.NotFound(new DocumentResponse
                    {
                        Success = false,
                        Message = $"Document '{documentId}' not found"
                    });
                }

                return Results.Ok(new DocumentResponse
                {
                    Success = true,
                    Message = $"Document '{documentId}' deleted successfully"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
                return Results.StatusCode(500);
            }
        })
        .WithName("DeleteDocument");

        endpoints.MapGet("/api/documents", async (
            IBlobStorageVault vault,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var documents = await vault.ListDocumentsAsync(ct);

                return Results.Ok(new
                {
                    Success = true,
                    Message = $"Retrieved {documents.Count()} document(s)",
                    Data = documents
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing documents");
                return Results.StatusCode(500);
            }
        })
        .WithName("ListDocuments");

        endpoints.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithName("HealthCheck");

        return endpoints;
    }
}
