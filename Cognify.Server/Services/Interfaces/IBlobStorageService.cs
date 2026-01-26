namespace Cognify.Server.Services.Interfaces;

public interface IBlobStorageService
{
    string GenerateUploadSasToken(string blobName, DateTimeOffset expiresOn);
    string GenerateDownloadSasToken(string blobName, DateTimeOffset expiresOn, string? originalFileName = null);
    Task DeleteAsync(string blobName);
}
