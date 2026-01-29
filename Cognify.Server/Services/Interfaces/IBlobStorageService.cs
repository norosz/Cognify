namespace Cognify.Server.Services.Interfaces;

public interface IBlobStorageService
{
    string GenerateUploadSasToken(string blobName, DateTimeOffset expiresOn);
    string GenerateDownloadSasToken(string blobName, DateTimeOffset expiresOn, string? originalFileName = null);
    Task<Stream> DownloadStreamAsync(string blobName);
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobName);
}
