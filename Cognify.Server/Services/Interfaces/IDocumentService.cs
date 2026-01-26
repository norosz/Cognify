using Cognify.Server.Dtos.Documents;

namespace Cognify.Server.Services.Interfaces;

public interface IDocumentService
{
    Task<DocumentDto> UploadDocumentAsync(Guid moduleId, Stream fileStream, string fileName, string contentType);
    Task<IEnumerable<DocumentDto>> GetModuleDocumentsAsync(Guid moduleId);
    Task DeleteDocumentAsync(Guid documentId);
}
