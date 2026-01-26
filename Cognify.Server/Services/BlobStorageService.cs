using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "documents";

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    private async Task<BlobContainerClient> GetContainerClientAsync()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        return containerClient;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType)
    {
        var containerClient = await GetContainerClientAsync();
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = containerClient.GetBlobClient(uniqueFileName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });

        return uniqueFileName; // Storing the path relative to the container
    }

    public async Task<Stream> DownloadAsync(string blobPath)
    {
        var containerClient = await GetContainerClientAsync();
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob {blobPath} not found.");
        }

        var downloadInfo = await blobClient.DownloadAsync();
        return downloadInfo.Value.Content;
    }

    public async Task DeleteAsync(string blobPath)
    {
        var containerClient = await GetContainerClientAsync();
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync();
    }
}
