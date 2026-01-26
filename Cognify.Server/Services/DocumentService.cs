using Cognify.Server.Data;
using Cognify.Server.Dtos.Documents;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserContextService _userContextService;

    public DocumentService(
        ApplicationDbContext context, 
        IBlobStorageService blobStorageService,
        IUserContextService userContextService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _userContextService = userContextService;
    }

    public async Task<DocumentDto> UploadDocumentAsync(Guid moduleId, Stream fileStream, string fileName, string contentType)
    {
        var userId = _userContextService.GetCurrentUserId();
        
        // Verify Module Ownership
        var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId);
        if (module == null)
        {
            throw new KeyNotFoundException($"Module {moduleId} not found.");
        }
        
        if (module.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("You don't own this module.");
        }

        // Upload to Blob Storage
        var blobPath = await _blobStorageService.UploadAsync(fileStream, fileName, contentType);

        // Save to Database
        var document = new Document
        {
            ModuleId = moduleId,
            BlobPath = blobPath,
            Status = DocumentStatus.Processing
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToDto(document);
    }

    public async Task<IEnumerable<DocumentDto>> GetModuleDocumentsAsync(Guid moduleId)
    {
        var userId = _userContextService.GetCurrentUserId();
        
        var module = await _context.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId);
        if (module == null) 
            throw new KeyNotFoundException($"Module {moduleId} not found.");

        if (module.OwnerUserId != userId) 
             throw new UnauthorizedAccessException("You don't own this module.");

        var documents = await _context.Documents
            .Where(d => d.ModuleId == moduleId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(MapToDto);
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
         var userId = _userContextService.GetCurrentUserId();
         
         var document = await _context.Documents
             .Include(d => d.Module)
             .FirstOrDefaultAsync(d => d.Id == documentId);

         if (document == null) return; // Idempotent

         if (document.Module!.OwnerUserId != userId)
             throw new UnauthorizedAccessException("You don't own this document.");

         // Delete from Blob Storage
         await _blobStorageService.DeleteAsync(document.BlobPath);

         // Delete from DB
         _context.Documents.Remove(document);
         await _context.SaveChangesAsync();
    }

    private static DocumentDto MapToDto(Document doc)
    {
        return new DocumentDto
        {
            Id = doc.Id,
            ModuleId = doc.ModuleId,
            BlobPath = doc.BlobPath,
            FileName = doc.BlobPath.Substring(doc.BlobPath.IndexOf('_') + 1), // Simple extraction assuming GUID_Name format
            Status = doc.Status,
            CreatedAt = doc.CreatedAt
        };
    }
}
