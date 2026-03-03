using Azure.Identity;
using Azure.Storage.Blobs;
using DocumentUploaderAPI.Services;

namespace DocumentUploaderAPI.Extensions;

public static class BlobStorageServiceExtensions
{
    public static async Task<WebApplicationBuilder> AddDocumentStorageAsync(this WebApplicationBuilder builder)
    {
        var blobConfig = builder.Configuration.GetSection("AzureBlob");
        var useEmulator = blobConfig.GetValue<bool>("UseEmulator");
        var containerName = blobConfig["ContainerName"] ?? "documents";

        BlobContainerClient containerClient;

        if (useEmulator)
        {
            var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
            containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            try
            {
                await containerClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Startup");
                logger.LogWarning("Could not connect to Azure Storage Emulator. Make sure Azurite is running on http://127.0.0.1:10000. Error: {Error}", ex.Message);
            }
        }
        else
        {
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
                var logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Startup");
                logger.LogWarning("Could not connect to Azure Storage. Error: {Error}", ex.Message);
            }
        }

        builder.Services.AddSingleton(containerClient);
        builder.Services.AddScoped<IBlobStorageVault, BlobStorageVault>();

        return builder;
    }
}
