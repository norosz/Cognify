using Cognify.Server.Data;
using Cognify.Server.Dtos.Notes;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class NoteService(ApplicationDbContext context, IUserContextService userContext, IBlobStorageService blobStorageService) : INoteService
{
    public async Task<List<NoteDto>> GetByModuleIdAsync(Guid moduleId)
    {
        var userId = userContext.GetCurrentUserId();

        // Ensure user owns the module
        var module = await context.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);

        if (module == null)
        {
            return new List<NoteDto>(); // Or throw Unauthorized/NotFound exception depending on preference
        }

        var notes = await context.Notes.AsNoTracking()
            .Where(n => n.ModuleId == moduleId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notes.Select(MapToDto).ToList();
    }

    public async Task<NoteDto?> GetByIdAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        
        var note = await context.Notes.AsNoTracking()
            .Include(n => n.Module)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
            return null;
        }

        return MapToDto(note);
    }

    public async Task<NoteSourcesDto?> GetSourcesAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();

        var note = await context.Notes.AsNoTracking()
            .Include(n => n.Module)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
            return null;
        }

        var documents = await context.Documents.AsNoTracking()
            .Where(d => d.ModuleId == note.ModuleId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var uploadedDocs = documents.Select(doc => new NoteSourceDocumentDto(
            doc.Id,
            ExtractFileName(doc.BlobPath),
            doc.Status.ToString(),
            doc.CreatedAt,
            doc.FileSize,
            doc.Status == Models.DocumentStatus.Uploaded
                ? blobStorageService.GenerateDownloadSasToken(doc.BlobPath, DateTimeOffset.UtcNow.AddHours(1), ExtractFileName(doc.BlobPath))
                : null
        )).ToList();

        var extracted = await context.ExtractedContents.AsNoTracking()
            .Include(e => e.Document)
            .Where(e => e.ModuleId == note.ModuleId && e.UserId == userId)
            .OrderByDescending(e => e.ExtractedAt)
            .ToListAsync();

        var extractedDocs = extracted.Select(extract => new NoteSourceExtractionDto(
            extract.Id,
            extract.DocumentId,
            extract.Document?.FileName ?? "Unknown",
            extract.ExtractedAt,
            extract.Status.ToString(),
            extract.IsSaved,
            extract.Document?.Status == Models.DocumentStatus.Uploaded && !string.IsNullOrWhiteSpace(extract.Document.BlobPath)
                ? blobStorageService.GenerateDownloadSasToken(
                    extract.Document.BlobPath,
                    DateTimeOffset.UtcNow.AddHours(1),
                    ExtractFileName(extract.Document.BlobPath))
                : null
        )).ToList();

        return new NoteSourcesDto
        {
            NoteId = note.Id,
            ModuleId = note.ModuleId,
            UploadedDocuments = uploadedDocs,
            ExtractedDocuments = extractedDocs
        };
    }

    public async Task<NoteDto> CreateAsync(CreateNoteDto dto)
    {
        var userId = userContext.GetCurrentUserId();

        // Verify module ownership
        var module = await context.Modules.FirstOrDefaultAsync(m => m.Id == dto.ModuleId && m.OwnerUserId == userId);
        if (module == null)
        {
            throw new UnauthorizedAccessException("User does not own this module."); // Or specialized exception
        }

        var note = new Note
        {
            ModuleId = dto.ModuleId,
            Title = dto.Title,
            UserContent = dto.UserContent ?? dto.Content,
            AiContent = dto.AiContent
        };

        note.Content = BuildLegacyContent(note.UserContent, note.AiContent, dto.Content);

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        return MapToDto(note);
    }

    public async Task<NoteDto?> UpdateAsync(Guid id, UpdateNoteDto dto)
    {
        var userId = userContext.GetCurrentUserId();

        var note = await context.Notes
            .Include(n => n.Module)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
            return null;
        }

        note.Title = dto.Title;

        if (dto.UserContent != null || dto.Content != null)
        {
            note.UserContent = dto.UserContent ?? dto.Content;
        }

        if (dto.AiContent != null)
        {
            note.AiContent = dto.AiContent;
        }

        note.Content = BuildLegacyContent(note.UserContent, note.AiContent, dto.Content);

        await context.SaveChangesAsync();

        return MapToDto(note);
    }

    public async Task<NoteDto?> UpdateExamInclusionAsync(Guid id, NoteExamInclusionDto dto)
    {
        var userId = userContext.GetCurrentUserId();

        var note = await context.Notes
            .Include(n => n.Module)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
            return null;
        }

        note.IncludeInFinalExam = dto.IncludeInFinalExam;
        await context.SaveChangesAsync();

        return MapToDto(note);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();

        var note = await context.Notes
            .Include(n => n.Module)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
            return false;
        }

        context.Notes.Remove(note);
        await context.SaveChangesAsync();
        return true;
    }

    private NoteDto MapToDto(Note note)
    {
        var embeddedImages = ParseEmbeddedImages(note.EmbeddedImagesJson, note.Title);

        return new NoteDto
        {
            Id = note.Id,
            ModuleId = note.ModuleId,
            SourceMaterialId = note.SourceMaterialId,
            Title = note.Title,
            Content = note.Content,
            UserContent = note.UserContent,
            AiContent = note.AiContent,
            IncludeInFinalExam = note.IncludeInFinalExam,
            CreatedAt = note.CreatedAt,
            EmbeddedImages = embeddedImages
        };
    }

    private static string? BuildLegacyContent(string? userContent, string? aiContent, string? fallback)
    {
        var segments = new List<string>();

        if (!string.IsNullOrWhiteSpace(userContent))
        {
            segments.Add(userContent);
        }

        if (!string.IsNullOrWhiteSpace(aiContent))
        {
            segments.Add(aiContent);
        }

        if (segments.Count == 0)
        {
            return fallback;
        }

        return string.Join("\n\n", segments);
    }

    private IReadOnlyList<NoteEmbeddedImageDto>? ParseEmbeddedImages(string? imagesJson, string title)
    {
        if (string.IsNullOrWhiteSpace(imagesJson)) return null;

        try
        {
            var images = JsonSerializer.Deserialize<List<EmbeddedImageRecord>>(imagesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (images == null || images.Count == 0) return null;

            return images.Select(i => new NoteEmbeddedImageDto(
                i.Id ?? Guid.NewGuid().ToString(),
                i.BlobPath ?? string.Empty,
                i.FileName ?? "embedded-image",
                i.PageNumber,
                string.IsNullOrWhiteSpace(i.BlobPath)
                    ? null
                    : blobStorageService.GenerateDownloadSasToken(
                        i.BlobPath,
                        DateTimeOffset.UtcNow.AddHours(1),
                        i.FileName ?? title)
            )).ToList();
        }
        catch
        {
            return null;
        }
    }

    private sealed record EmbeddedImageRecord(
        string? Id,
        string? BlobPath,
        string? FileName,
        int PageNumber
    );

    private static string ExtractFileName(string blobPath)
    {
        var index = blobPath.IndexOf('_');
        return index >= 0 ? blobPath[(index + 1)..] : blobPath;
    }
}
