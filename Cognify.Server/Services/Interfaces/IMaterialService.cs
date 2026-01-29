using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IMaterialService
{
    Task<Material> EnsureForDocumentAsync(Guid documentId, Guid userId);
    Task<Material?> GetByDocumentIdAsync(Guid documentId, Guid userId);
}