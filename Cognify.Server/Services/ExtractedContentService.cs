using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class ExtractedContentService(ApplicationDbContext db, IAgentRunService agentRunService, IMaterialService materialService) : IExtractedContentService
{
    public async Task<ExtractedContent> CreatePendingAsync(Guid userId, Guid documentId, Guid moduleId)
    {
        var inputHash = AgentRunService.ComputeHash($"extraction:{Cognify.Server.Dtos.Ai.Contracts.AgentContractVersions.V2}:{userId}:{documentId}");

        // Check for existing unsaved content to prevent duplicates (e.g. retries)
        var existing = await db.ExtractedContents
            .FirstOrDefaultAsync(e => e.DocumentId == documentId && e.UserId == userId && !e.IsSaved);

        if (existing != null)
        {
            var run = await agentRunService.CreateAsync(userId, AgentRunType.Extraction, inputHash, promptVersion: "extract-v2");
            existing.AgentRunId = run.Id;
            existing.Status = ExtractedContentStatus.Processing;
            existing.ErrorMessage = null;
            existing.ExtractedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return existing;
        }

        var agentRun = await agentRunService.CreateAsync(userId, AgentRunType.Extraction, inputHash, promptVersion: "extract-v2");

        var content = new ExtractedContent
        {
            UserId = userId,
            DocumentId = documentId,
            ModuleId = moduleId,
            AgentRunId = agentRun.Id,
            Status = ExtractedContentStatus.Processing,
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
            content.Status = ExtractedContentStatus.Ready;
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
            content.Status = ExtractedContentStatus.Error;
            content.ErrorMessage = errorMessage;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<ExtractedContent>> GetByUserAsync(Guid userId)
    {
        return await db.ExtractedContents
            .Include(e => e.Document)
            .Include(e => e.Module)
            .Include(e => e.AgentRun)
            .Where(e => e.UserId == userId && !e.IsSaved)
            .OrderByDescending(e => e.ExtractedAt)
            .ToListAsync();
    }

    public async Task<ExtractedContent?> GetByIdAsync(Guid id)
    {
        return await db.ExtractedContents
            .Include(e => e.Document)
            .Include(e => e.Module)
            .Include(e => e.AgentRun)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Note> SaveAsNoteAsync(Guid extractedContentId, Guid userId, string title)
    {
        var content = await db.ExtractedContents
            .Include(e => e.AgentRun)
            .FirstOrDefaultAsync(e => e.Id == extractedContentId)
            ?? throw new InvalidOperationException("Extracted content not found.");

        if (content.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this content.");

        var material = await materialService.GetByDocumentIdAsync(content.DocumentId, userId);
        var imagesJson = ExtractImagesJson(content.AgentRun?.OutputJson);

        var note = new Note
        {
            ModuleId = content.ModuleId,
            Title = title,
            Content = content.Text,
            SourceMaterialId = material?.Id,
            EmbeddedImagesJson = imagesJson
        };

        db.Notes.Add(note);

        // Mark as saved (soft delete)
        content.IsSaved = true;

        await db.SaveChangesAsync();
        return note;
    }

    private static string? ExtractImagesJson(string? outputJson)
    {
        if (string.IsNullOrWhiteSpace(outputJson)) return null;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(outputJson);
            if (doc.RootElement.TryGetProperty("images", out var images) && images.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                return images.GetRawText();
            }
        }
        catch
        {
            return null;
        }

        return null;
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
