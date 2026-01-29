using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class MaterialService(ApplicationDbContext db) : IMaterialService
{
    public async Task<Material> EnsureForDocumentAsync(Guid documentId, Guid userId)
    {
        var existing = await db.Materials.FirstOrDefaultAsync(m => m.SourceDocumentId == documentId && m.UserId == userId);
        if (existing != null)
        {
            if (existing.Status == MaterialStatus.Failed)
            {
                existing.Status = MaterialStatus.Uploaded;
            }

            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return existing;
        }

        var document = await db.Documents
            .Include(d => d.Module)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.Module == null || document.Module.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Document not found or access denied.");
        }

        var material = new Material
        {
            UserId = userId,
            ModuleId = document.ModuleId,
            SourceDocumentId = document.Id,
            FileName = document.FileName,
            ContentType = GetContentType(document.FileName),
            BlobPath = document.BlobPath,
            Status = MaterialStatus.Uploaded,
            HasEmbeddedImages = false
        };

        db.Materials.Add(material);
        await db.SaveChangesAsync();
        return material;
    }

    public async Task<Material?> GetByDocumentIdAsync(Guid documentId, Guid userId)
    {
        return await db.Materials
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.SourceDocumentId == documentId && m.UserId == userId);
    }

    private static string? GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }
}