using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    private const string ContainerName = "documents";

    public string GenerateUploadSasToken(string blobName, DateTimeOffset expiresOn)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlobClient(blobName);

        var sas = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresOn
        };

        sas.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        return blob.GenerateSasUri(sas).ToString();
    }

    public string GenerateDownloadSasToken(string blobName, DateTimeOffset expiresOn, string? originalFileName = null)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlobClient(blobName);

        var sas = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresOn
        };

        if (!string.IsNullOrEmpty(originalFileName))
        {
            sas.ContentDisposition = $"attachment; filename={originalFileName}";
        }

        sas.SetPermissions(BlobSasPermissions.Read);

        return blob.GenerateSasUri(sas).ToString();
    }

    public async Task DeleteAsync(string blobName)
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }
}
