using DocumentUploaderAPI.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddLogging();

// Add Auth0 authentication
builder.Services.AddAuth0Authentication(builder.Configuration);

builder.Services.AddCors();

await builder.AddDocumentStorageAsync();

var app = builder.Build();

// Log which storage backend is being used
var storageLogger = app.Services.GetRequiredService<ILogger<Program>>();
var blobConfig = app.Configuration.GetSection("AzureBlob");
var useEmulator = blobConfig.GetValue<bool>("UseEmulator");
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
//     app.UseSwaggerUI(options =>
// {
//     options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
// });    
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Configure CORS for frontend apps
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapDocumentEndpoints();

app.Run();