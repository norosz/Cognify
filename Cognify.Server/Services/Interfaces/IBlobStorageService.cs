using System.IO;
using System.Threading.Tasks;

namespace Cognify.Server.Services.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType);
    Task<Stream> DownloadAsync(string blobPath);
    Task DeleteAsync(string blobPath);
}
