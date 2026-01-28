using Cognify.Server.Dtos.Documents;

namespace Cognify.Server.Services.Interfaces;

public interface IDocumentService
{
    Task<UploadInitiateResponse> InitiateUploadAsync(Guid moduleId, string fileName, string contentType, long fileSize);
    Task<DocumentDto> CompleteUploadAsync(Guid documentId);
    Task DeleteDocumentAsync(Guid documentId);
    Task<IEnumerable<DocumentDto>> GetModuleDocumentsAsync(Guid moduleId);
    Task<DocumentDto?> GetByIdAsync(Guid documentId);
}
