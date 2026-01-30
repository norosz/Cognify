using Cognify.Server.Data;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class ModuleService(ApplicationDbContext context, IUserContextService userContext, IAiService aiService) : IModuleService
{
    public async Task<List<ModuleDto>> GetModulesAsync()
    {
        var userId = userContext.GetCurrentUserId();
        return await context.Modules
            .Where(m => m.OwnerUserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ModuleDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                CreatedAt = m.CreatedAt,
                DocumentsCount = m.Documents.Count(),
                NotesCount = m.Notes.Count(),
                QuizzesCount = m.Notes.SelectMany(n => n.Quizzes).Count(),
                CategoryLabel = m.CategoryLabel,
                CategorySource = m.CategorySource
            })
            .ToListAsync();
    }

    public async Task<ModuleDto?> GetModuleAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules
            .Include(m => m.Documents)
            .Include(m => m.Notes)
            .ThenInclude(n => n.Quizzes)
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerUserId == userId);

        if (module == null) return null;

        return new ModuleDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            CreatedAt = module.CreatedAt,
            DocumentsCount = module.Documents.Count,
            NotesCount = module.Notes.Count,
            QuizzesCount = module.Notes.SelectMany(n => n.Quizzes).Count(),
            CategoryLabel = module.CategoryLabel,
            CategorySource = module.CategorySource
        };
    }

    public async Task<ModuleDto> CreateModuleAsync(CreateModuleDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        var categoryLabel = dto.CategoryLabel?.Trim();
        var categorySource = string.IsNullOrWhiteSpace(categoryLabel) ? null : "User";

        if (string.IsNullOrWhiteSpace(categoryLabel))
        {
            var contextText = BuildModuleCreationContext(dto.Title, dto.Description);
            var suggestions = await aiService.SuggestCategoriesAsync(contextText, 3);
            categoryLabel = suggestions.Suggestions.FirstOrDefault()?.Label?.Trim();

            if (string.IsNullOrWhiteSpace(categoryLabel)
                || string.Equals(categoryLabel, "Uncategorized", StringComparison.OrdinalIgnoreCase))
            {
                categoryLabel = "General";
            }

            categorySource = "AI";
        }

        var module = new Module
        {
            Title = dto.Title,
            Description = dto.Description,
            OwnerUserId = userId,
            CategoryLabel = categoryLabel,
            CategorySource = categorySource
        };

        context.Modules.Add(module);
        await context.SaveChangesAsync();

        return new ModuleDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            CreatedAt = module.CreatedAt,
            CategoryLabel = module.CategoryLabel,
            CategorySource = module.CategorySource
        };
    }

    public async Task<ModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerUserId == userId);

        if (module == null) return null;

        module.Title = dto.Title;
        module.Description = dto.Description;

        await context.SaveChangesAsync();

        return new ModuleDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            CreatedAt = module.CreatedAt,
            CategoryLabel = module.CategoryLabel,
            CategorySource = module.CategorySource
        };
    }

    public async Task<bool> DeleteModuleAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerUserId == userId);

        if (module == null) return false;

        context.Modules.Remove(module);
        await context.SaveChangesAsync();
        return true;
    }

    private static string BuildModuleCreationContext(string title, string? description)
    {
        var builder = new List<string>
        {
            $"Module title: {title}"
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.Add($"Description: {description}");
        }

        builder.Add("Suggest a concise academic category for this module.");
        return string.Join("\n", builder);
    }
}
