using Azure.Storage.Sas;
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

    public async Task<UploadInitiateResponse> InitiateUploadAsync(Guid moduleId, string fileName, string contentType)
    {
        var userId = _userContextService.GetCurrentUserId();
        
        // Verify Module Ownership
        var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId);
        
        if (module == null)
            throw new KeyNotFoundException("Module not found");

        if (module.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You are not the owner of this module");

        // Create Pending Document
        var documentId = Guid.NewGuid();
        var blobName = $"{moduleId}/{documentId}_{fileName}";

        var document = new Document
        {
            Id = documentId,
            ModuleId = moduleId,
            FileName = fileName,
            BlobPath = blobName,
            CreatedAt = DateTime.UtcNow,
            Status = Models.DocumentStatus.Processing 
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Generate SAS Token for Upload (Write)
        // Expiry: 15 minutes
        var sasUrl = _blobStorageService.GenerateUploadSasToken(blobName, DateTimeOffset.UtcNow.AddMinutes(15));
        return new UploadInitiateResponse(documentId, sasUrl, blobName);

    }

    public async Task<DocumentDto> CompleteUploadAsync(Guid documentId)
    {
        var userId = _userContextService.GetCurrentUserId();

        var document = await _context.Documents.Include(d => d.Module).FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null)
            throw new KeyNotFoundException("Document not found");

        if (document.Module!.OwnerUserId != userId)
             throw new UnauthorizedAccessException("Unauthorized");

        // Ideally verify blob exists here?
        // For now, assume client succeeded if they called complete.
        
        document.Status = Models.DocumentStatus.Ready;
        await _context.SaveChangesAsync();

        var fileName = ExtractFileName(document.BlobPath);

        // Generate Read SAS for return
        var downloadUrl = _blobStorageService.GenerateDownloadSasToken(
            document.BlobPath,
            DateTimeOffset.UtcNow.AddHours(1),
            fileName
        );

        return new DocumentDto(
            document.Id, 
            document.ModuleId, 
            fileName, 
            document.BlobPath, 
            Dtos.Documents.DocumentStatus.Ready, 
            document.CreatedAt,
            downloadUrl
        );
    }

    public async Task<IEnumerable<DocumentDto>> GetModuleDocumentsAsync(Guid moduleId)
    {
        var userId = _userContextService.GetCurrentUserId();
        
        var module = await _context.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId);
        if (module == null) 
            throw new KeyNotFoundException("Module not found");

        if (module.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not have access to this module");

        var documents = await _context.Documents
            .Where(d => d.ModuleId == moduleId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var dtos = new List<DocumentDto>();
        foreach (var doc in documents)
        {
            var fileName = ExtractFileName(doc.BlobPath);
            string? downloadUrl = null;
            if (doc.Status == Models.DocumentStatus.Ready)
            {
                downloadUrl = _blobStorageService.GenerateDownloadSasToken(
                    doc.BlobPath,
                    DateTimeOffset.UtcNow.AddHours(1),
                    fileName // Pass filename for Content-Disposition
                );
            }

            var statusDto = (Dtos.Documents.DocumentStatus)(int)doc.Status; 

            dtos.Add(new DocumentDto(
                doc.Id,
                doc.ModuleId,
                fileName,
                doc.BlobPath,
                statusDto,
                doc.CreatedAt,
                downloadUrl
            ));
        }

        return dtos;
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid documentId)
    {
        var userId = _userContextService.GetCurrentUserId();
        
        var document = await _context.Documents
            .Include(d => d.Module)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null) return null;
        if (document.Module!.OwnerUserId != userId) return null; // Or throw

        var fileName = ExtractFileName(document.BlobPath);
        var downloadUrl = document.Status == Models.DocumentStatus.Ready 
            ? _blobStorageService.GenerateDownloadSasToken(document.BlobPath, DateTimeOffset.UtcNow.AddHours(1), fileName) 
            : null;

        return new DocumentDto(
            document.Id, 
            document.ModuleId, 
            fileName, 
            document.BlobPath, 
            (Dtos.Documents.DocumentStatus)document.Status, 
            document.CreatedAt, 
            downloadUrl
        );
    }

    private string ExtractFileName(string blobPath)
    {
        // format: moduleId/guid_filename
        var index = blobPath.IndexOf('_');
        return index >= 0 ? blobPath.Substring(index + 1) : blobPath;
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

}
