using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IExtractedContentService
{
    Task<ExtractedContent> CreatePendingAsync(Guid userId, Guid documentId, Guid moduleId);
    Task UpdateAsync(Guid id, string text);
    Task MarkAsErrorAsync(Guid id, string errorMessage);
    Task<List<ExtractedContent>> GetByUserAsync(Guid userId);
    Task<ExtractedContent?> GetByIdAsync(Guid id);
    Task<Note> SaveAsNoteAsync(Guid extractedContentId, Guid userId, string title);
    Task DeleteAsync(Guid id);
    Task<int> GetCountByUserAsync(Guid userId);
}
