# Document Uploader API

A minimal API built with .NET 10 for document management using Azure Blob Storage. This API implements the Vault Pattern to abstract blob storage operations and provides RESTful endpoints for document upload, retrieval, listing, and deletion.

## Architecture

The project follows the **Vault Pattern** for abstraction:

- **IBlobStorageVault**: Interface defining the contract for blob storage operations
- **BlobStorageVault**: Concrete implementation handling Azure Blob Storage interactions
- **DefaultAzureCredential**: Secure authentication without connection strings
- **Minimal API Endpoints**: Direct endpoint mapping in Program.cs for a lightweight architecture

## Prerequisites

- .NET 10 SDK or later
- Azure Storage Account (or Azure Storage Emulator for development)
- Visual Studio Code, Visual Studio, or any .NET IDE

## Getting Started

### 1. Configure Azure Blob Storage

The API uses **DefaultAzureCredential** for authentication, which supports multiple authentication methods:
- Managed Identity (recommended for Azure)
- Azure CLI authentication
- Visual Studio authentication
- Environment variables
- Interactive browser login

#### For Production (with Managed Identity):
Update `appsettings.json` with your Storage Account URI:

```json
{
  "AzureBlob": {
    "StorageAccountUri": "https://<your-account>.blob.core.windows.net",
    "ContainerName": "documents"
  }
}
```

Then assign the required RBAC roles to your Managed Identity:
- `Storage Blob Data Contributor` - For upload, download, delete
- `Storage Blob Data Reader` - For read-only access

#### For Development (using Azure Storage Emulator):
The `appsettings.Development.json` is pre-configured for local development:

```json
{
  "AzureBlob": {
    "StorageAccountUri": "http://127.0.0.1:10000/devstoreaccount1",
    "ContainerName": "documents"
  }
}
```

To use the development storage, install and run [Azure Storage Emulator](https://docs.microsoft.com/azure/storage/common/storage-use-emulator) or [Azurite](https://github.com/Azure/Azurite).

### 2. Install Dependencies

```bash
cd DocumentUploaderAPI
dotnet restore
```

### 3. Run the Application

```bash
dotnet run
```

The API will start on `https://localhost:5149` (HTTPS) or `http://localhost:5000` depending on your launch settings.

## API Endpoints

### 1. Health Check
```http
GET /health
```
**Response:**
```json
{
  "status": "healthy"
}
```

### 2. List All Documents
```http
GET /api/documents
```
**Response:**
```json
{
  "success": true,
  "message": "Retrieved 3 document(s)",
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000_document.pdf",
      "fileName": "document.pdf",
      "sizeInBytes": 1024000,
      "contentType": "application/pdf",
      "uploadedAt": "2026-03-03T10:30:00Z",
      "uri": "https://youraccount.blob.core.windows.net/documents/..."
    }
  ]
}
```

### 3. Upload Document(s)
```http
POST /api/documents/upload
Content-Type: multipart/form-data

[file1] [file2] [file3]...
```

**Request:** Form data with one or more files
**Response:**
```json
{
  "success": true,
  "message": "Successfully uploaded 1 document(s)",
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000_myfile.pdf",
      "fileName": "myfile.pdf",
      "sizeInBytes": 2048000,
      "contentType": "application/pdf",
      "uploadedAt": "2026-03-03T10:45:00Z",
      "uri": "https://youraccount.blob.core.windows.net/documents/..."
    }
  ]
}
```

### 4. Get Document
```http
GET /api/documents/{documentId}
```

**Response:** The document file as binary stream with appropriate content-type header.

### 5. Delete Document
```http
DELETE /api/documents/{documentId}
```

**Response:**
```json
{
  "success": true,
  "message": "Document '550e8400-e29b-41d4-a716-446655440000_myfile.pdf' deleted successfully"
}
```

## Models

### DocumentInfo
Represents metadata information about a stored document:
- `id`: Unique identifier (blob name)
- `fileName`: Original file name
- `sizeInBytes`: File size in bytes
- `contentType`: MIME type
- `uploadedAt`: Upload timestamp
- `uri`: URI to access the document

### UploadDocumentRequest
Request model for uploading documents:
- `file`: IFormFile to upload
- `metadata`: Optional metadata

### DocumentResponse
Response model for API operations:
- `success`: Operation result
- `message`: Description of the operation
- `data`: Document data (if applicable)

## Testing

### Using REST Client Extension (VS Code)
Open `DocumentUploaderAPI.http` and use the provided request templates.

### Using curl

List documents:
```bash
curl -X GET "https://localhost:5149/api/documents"
```

Upload a document:
```bash
curl -X POST "https://localhost:5149/api/documents/upload" \
  -F "files=@/path/to/document.pdf"
```

Get a document:
```bash
curl -X GET "https://localhost:5149/api/documents/{documentId}" \
  -o downloaded_document.pdf
```

Delete a document:
```bash
curl -X DELETE "https://localhost:5149/api/documents/{documentId}"
```

## Development

### Project Structure
```
DocumentUploaderAPI/
├── Models/
│   └── DocumentInfo.cs          # Data models for documents
├── Services/
│   ├── IBlobStorageVault.cs     # Vault pattern interface
│   └── BlobStorageVault.cs      # Azure Blob Storage implementation
├── Program.cs                    # API configuration and endpoints
├── appsettings.json              # Production settings
├── appsettings.Development.json  # Development settings
└── DocumentUploaderAPI.http      # HTTP request examples
```

### Key Features
- **Vault Pattern**: Abstraction layer for blob storage operations
- **Minimal API**: Lightweight endpoint configuration
- **Async/Await**: Non-blocking I/O operations
- **Error Handling**: Comprehensive exception handling and logging
- **CORS Support**: Pre-configured for frontend applications
- **Metadata Support**: Optional metadata with uploaded documents
- **Cancellation Tokens**: Support for graceful cancellation

## Environment Variables
use environment variables for authentication configuration:

```bash
# For local development, use Azure CLI login (recommended)
az login

# Or sign in with Visual Studio
# Visual Studio will automatically detect credentials

# Or set specific environment variables for service principal
$env:AZURE_CLIENT_ID = "your-client-id"
$envNo Connection Strings**: DefaultAzureCredential eliminates the need for storing account keys
- **Managed Identity**: Recommended authentication method for Azure resources
- **RBAC**: Use least-privilege role assignments (Storage Blob Data Contributor/Reader)
- **CORS**: Configure appropriate CORS policies for production
- **Authentication**: Consider adding API key or OAuth middleware for the endpoint
For configuration overrides:
```bash
# Azure Blob Storage settings
$env:AzureBlob__StorageAccountUri = "https://your-account.blob.core.windows.net
$env:AzureBlob__ConnectionString = "your-connection-string"
$env:AzureBlob__ContainerName = "documents"
```

## Security Considerations

- **Connection Strings**: Store sensitive data in Azure Key Vault
- **CORS**: Configure appropriate CORS policies for production
- **Authentication**: Consider adding authentication/authorization middleware
- **Container Access**: Use Shared Access Signatures (SAS) for limited access
- **File Validation**: Implement file type and size validation in production

## Deployment

For deployment to Azure, refer to [Azure Prepare](#) skill for infrastructure setup and [Azure Deploy](#) skill for deployment instructions.

## License

This project is licensed under the MIT License.
