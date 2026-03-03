using Azure.Storage.Blobs;
using Azure.Identity;
using DocumentUploaderAPI.Models;
using DocumentUploaderAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddLogging();
builder.Services.AddCors();

// Configure Azure Blob Storage
var blobConfig = builder.Configuration.GetSection("AzureBlob");
var useEmulator = blobConfig.GetValue<bool>("UseEmulator");
var containerName = blobConfig["ContainerName"] ?? "documents";

BlobContainerClient containerClient;

if (useEmulator)
{
    // Use Azure Storage Emulator (Azurite)
    string connectionString = "UseDevelopmentStorage=true";
    var blobServiceClient = new BlobServiceClient(connectionString);
    containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    
    // Try to create container, but don't fail if emulator isn't running yet
    try
    {
        await containerClient.CreateIfNotExistsAsync();
    }
    catch (Exception ex)
    {
        builder.Logging.AddConsole();
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
        logger.LogWarning("Could not connect to Azure Storage Emulator. Make sure Azurite is running on http://127.0.0.1:10000. Error: {Error}", ex.Message);
    }
}
else
{
    // Use Azure Blob Storage with DefaultAzureCredential
    var storageUri = blobConfig["StorageAccountUri"];

    if (string.IsNullOrEmpty(storageUri))
    {
        throw new InvalidOperationException("Azure Blob Storage URI not configured. Please set 'AzureBlob:StorageAccountUri' in appsettings.json");
    }

    var credential = new DefaultAzureCredential();
    var blobServiceClient = new BlobServiceClient(new Uri(storageUri), credential);
    containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    
    try
    {
        await containerClient.CreateIfNotExistsAsync();
    }
    catch (Exception ex)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
        logger.LogWarning("Could not connect to Azure Storage. Error: {Error}", ex.Message);
    }
}

// Register Azure Blob Storage service
builder.Services.AddSingleton(containerClient);
builder.Services.AddScoped<IBlobStorageVault, BlobStorageVault>();

var app = builder.Build();

// Log which storage backend is being used
var storageLogger = app.Services.GetRequiredService<ILogger<Program>>();
if (useEmulator)
{
    storageLogger.LogInformation("Using Azure Storage Emulator (Azurite). Ensure it's running on http://127.0.0.1:10000");
}
else
{
    storageLogger.LogInformation("Using Azure Blob Storage at: {Uri}", blobConfig["StorageAccountUri"]);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Configure CORS for frontend apps
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Document Management Endpoints

/// <summary>
/// Upload a document to blob storage.
/// </summary>
app.MapPost("/api/documents/upload", async (
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
.WithName("UploadDocuments");

/// <summary>
/// Retrieve a document from blob storage, returns as file download.
/// </summary>
app.MapGet("/api/documents/{documentId}", async (
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

/// <summary>
/// Delete a document from blob storage.
/// </summary>
app.MapDelete("/api/documents/{documentId}", async (
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
/// <summary>
/// List all documents in blob storage.
/// </summary>
app.MapGet("/api/documents", async (
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
/// <summary>
/// Health check endpoint.
/// </summary>
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
.WithName("HealthCheck");

app.Run();