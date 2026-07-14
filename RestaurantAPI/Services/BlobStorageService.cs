using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RestaurantAPI.ServiceInterfaces;

namespace RestaurantAPI.Services;

public class BlobStorageService:IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _logger = logger;

        var connectionString =
            configuration["BlobStorage:ConnectionString"]
                ?? throw new Exception("Blob Storage connection string not found.");

        _containerClient = new BlobContainerClient(
            connectionString,
            "menu-images");
    }

    public async Task<string> UploadMenuImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new Exception("Image file is required.");
        }
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var blobClient = _containerClient.GetBlobClient(fileName);

        await using var stream = file.OpenReadStream();

        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders
            {
                ContentType = file.ContentType
            });

        _logger.LogInformation(
            "Menu image uploaded successfully. Blob: {BlobName}",
            fileName);

        return blobClient.Uri.ToString();
    }
}
