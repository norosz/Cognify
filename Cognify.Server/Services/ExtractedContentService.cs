using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class ExtractedContentService(ApplicationDbContext db) : IExtractedContentService
{
    public async Task<ExtractedContent> CreatePendingAsync(Guid userId, Guid documentId, Guid moduleId)
    {
        // Check for existing unsaved content to prevent duplicates (e.g. retries)
        var existing = await db.ExtractedContents
            .FirstOrDefaultAsync(e => e.DocumentId == documentId && e.UserId == userId && !e.IsSaved);

        if (existing != null)
        {
            existing.Status = "Processing";
            existing.ErrorMessage = null;
            existing.ExtractedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return existing;
        }

        var content = new ExtractedContent
        {
            UserId = userId,
            DocumentId = documentId,
            ModuleId = moduleId,
            Status = "Processing",
            Text = null 
        };

        db.ExtractedContents.Add(content);
        await db.SaveChangesAsync();
        return content;
    }

    public async Task UpdateAsync(Guid id, string text)
    {
        var content = await db.ExtractedContents.FindAsync(id);
        if (content != null)
        {
            content.Text = text;
            content.Status = "Ready";
            content.ErrorMessage = null;
            content.ExtractedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAsErrorAsync(Guid id, string errorMessage)
    {
        var content = await db.ExtractedContents.FindAsync(id);
        if (content != null)
        {
            content.Status = "Error";
            content.ErrorMessage = errorMessage;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<ExtractedContent>> GetByUserAsync(Guid userId)
    {
        return await db.ExtractedContents
            .Include(e => e.Document)
            .Include(e => e.Module)
            .Where(e => e.UserId == userId && !e.IsSaved)
            .OrderByDescending(e => e.ExtractedAt)
            .ToListAsync();
    }

    public async Task<ExtractedContent?> GetByIdAsync(Guid id)
    {
        return await db.ExtractedContents
            .Include(e => e.Document)
            .Include(e => e.Module)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Note> SaveAsNoteAsync(Guid extractedContentId, Guid userId, string title)
    {
        var content = await db.ExtractedContents.FindAsync(extractedContentId)
            ?? throw new InvalidOperationException("Extracted content not found.");

        if (content.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this content.");

        var note = new Note
        {
            ModuleId = content.ModuleId,
            Title = title,
            Content = content.Text
        };

        db.Notes.Add(note);

        // Mark as saved (soft delete)
        content.IsSaved = true;

        await db.SaveChangesAsync();
        return note;
    }

    public async Task DeleteAsync(Guid id)
    {
        var content = await db.ExtractedContents.FindAsync(id);
        if (content != null)
        {
            db.ExtractedContents.Remove(content);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await db.ExtractedContents
            .Where(e => e.UserId == userId && !e.IsSaved)
            .CountAsync();
    }
}
