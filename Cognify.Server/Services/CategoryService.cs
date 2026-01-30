using System.Text;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Categories;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class CategoryService(ApplicationDbContext context, IUserContextService userContext, IAiService aiService) : ICategoryService
{
    public async Task<CategorySuggestionResponse?> SuggestModuleCategoriesAsync(Guid moduleId, int maxSuggestions)
    {
        var userId = userContext.GetCurrentUserId();

        var module = await context.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);

        if (module == null)
        {
            return null;
        }

        var noteCount = await context.Notes
            .AsNoTracking()
            .Where(n => n.ModuleId == moduleId)
            .CountAsync();

        var quizCount = await context.Quizzes
            .AsNoTracking()
            .Join(context.Notes.AsNoTracking(), q => q.NoteId, n => n.Id, (q, n) => new { q, n })
            .Where(x => x.n.ModuleId == moduleId)
            .CountAsync();

        if (noteCount + quizCount < 1)
        {
            throw new InvalidOperationException("Category suggestions are available after adding at least one note or quiz.");
        }

        var noteTitles = await context.Notes
            .AsNoTracking()
            .Where(n => n.ModuleId == moduleId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => n.Title)
            .ToListAsync();

        var quizTitles = await context.Quizzes
            .AsNoTracking()
            .Join(context.Notes.AsNoTracking(), q => q.NoteId, n => n.Id, (q, n) => new { q, n })
            .Where(x => x.n.ModuleId == moduleId)
            .OrderByDescending(x => x.q.CreatedAt)
            .Select(x => x.q.Title)
            .ToListAsync();

        var contextText = BuildModuleContext(module.Title, module.Description, noteTitles, quizTitles);
        var suggestions = await aiService.SuggestCategoriesAsync(contextText, maxSuggestions);
        await PersistHistoryBatchAsync(userId, moduleId, null, "AI", suggestions.Suggestions);
        return suggestions;
    }

    public async Task<CategorySuggestionResponse?> SuggestQuizCategoriesAsync(Guid quizId, int maxSuggestions)
    {
        var userId = userContext.GetCurrentUserId();

        var quiz = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null || quiz.Note?.Module?.OwnerUserId != userId)
        {
            return null;
        }

        if (quiz.Questions.Count < 3)
        {
            throw new InvalidOperationException("Category suggestions are available after the quiz has at least 3 questions.");
        }

        var contextText = BuildQuizContext(
            quiz.Title,
            quiz.Difficulty.ToString(),
            quiz.Type.ToString(),
            quiz.Questions.Select(q => q.Prompt).Take(3).ToList());

        var suggestions = await aiService.SuggestCategoriesAsync(contextText, maxSuggestions);
        await PersistHistoryBatchAsync(userId, null, quizId, "AI", suggestions.Suggestions);
        return suggestions;
    }

    public async Task<bool> SetModuleCategoryAsync(Guid moduleId, string categoryLabel)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);
        if (module == null)
        {
            return false;
        }

        module.CategoryLabel = categoryLabel;
        module.CategorySource = "User";
        await context.SaveChangesAsync();
        await PersistHistoryBatchAsync(userId, moduleId, null, "Applied", [new CategorySuggestionItemDto { Label = categoryLabel }]);
        return true;
    }

    public async Task<bool> SetQuizCategoryAsync(Guid quizId, string categoryLabel)
    {
        var userId = userContext.GetCurrentUserId();
        var quiz = await context.Quizzes
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.Note!.Module!.OwnerUserId == userId);

        if (quiz == null)
        {
            return false;
        }

        quiz.CategoryLabel = categoryLabel;
        quiz.CategorySource = "User";
        await context.SaveChangesAsync();
        await PersistHistoryBatchAsync(userId, null, quizId, "Applied", [new CategorySuggestionItemDto { Label = categoryLabel }]);
        return true;
    }

    public async Task<CategoryHistoryResponseDto?> GetModuleCategoryHistoryAsync(Guid moduleId, int take, Guid? cursor)
    {
        var userId = userContext.GetCurrentUserId();

        var module = await context.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);

        if (module == null)
        {
            return null;
        }

        return await GetHistoryAsync(userId, moduleId, null, take, cursor);
    }

    public async Task<CategoryHistoryResponseDto?> GetQuizCategoryHistoryAsync(Guid quizId, int take, Guid? cursor)
    {
        var userId = userContext.GetCurrentUserId();

        var quiz = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null || quiz.Note?.Module?.OwnerUserId != userId)
        {
            return null;
        }

        return await GetHistoryAsync(userId, null, quizId, take, cursor);
    }

    private async Task PersistHistoryBatchAsync(Guid userId, Guid? moduleId, Guid? quizId, string source, IEnumerable<CategorySuggestionItemDto> items)
    {
        var batch = new Models.CategorySuggestionBatch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ModuleId = moduleId,
            QuizId = quizId,
            Source = source,
            CreatedAt = DateTime.UtcNow,
            Items = items.Select(i => new Models.CategorySuggestionItem
            {
                Id = Guid.NewGuid(),
                Label = i.Label,
                Confidence = i.Confidence,
                Rationale = i.Rationale
            }).ToList()
        };

        context.CategorySuggestionBatches.Add(batch);
        await context.SaveChangesAsync();
    }

    private async Task<CategoryHistoryResponseDto> GetHistoryAsync(Guid userId, Guid? moduleId, Guid? quizId, int take, Guid? cursor)
    {
        var batches = context.CategorySuggestionBatches
            .AsNoTracking()
            .Include(b => b.Items)
            .Where(b => b.UserId == userId && b.ModuleId == moduleId && b.QuizId == quizId);

        if (cursor.HasValue)
        {
            var cursorBatch = await context.CategorySuggestionBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == cursor.Value && b.UserId == userId);

            if (cursorBatch == null)
            {
                throw new ArgumentException("Invalid cursor.");
            }

            batches = batches.Where(b => b.CreatedAt < cursorBatch.CreatedAt
                                         || (b.CreatedAt == cursorBatch.CreatedAt && b.Id.CompareTo(cursorBatch.Id) < 0));
        }

        var results = await batches
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Take(take)
            .ToListAsync();

        var response = new CategoryHistoryResponseDto
        {
            Items = results.Select(b => new CategoryHistoryBatchDto
            {
                BatchId = b.Id,
                CreatedAt = b.CreatedAt,
                Source = b.Source,
                Items = b.Items.Select(i => new CategoryHistoryItemDto
                {
                    Label = i.Label,
                    Confidence = i.Confidence,
                    Rationale = i.Rationale
                }).ToList()
            }).ToList(),
            NextCursor = results.Count == take ? results.Last().Id : null
        };

        return response;
    }

    private static string BuildModuleContext(string title, string? description, List<string> noteTitles, List<string> quizTitles)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Module: {title}");
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine($"Description: {description}");
        }

        if (noteTitles.Count > 0)
        {
            builder.AppendLine("Notes:");
            builder.AppendLine(string.Join(", ", noteTitles));
        }

        if (quizTitles.Count > 0)
        {
            builder.AppendLine("Quizzes:");
            builder.AppendLine(string.Join(", ", quizTitles));
        }

        return builder.ToString();
    }

    private static string BuildQuizContext(string title, string difficulty, string type, List<string> prompts)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Quiz: {title}");
        builder.AppendLine($"Difficulty: {difficulty}");
        builder.AppendLine($"Type: {type}");

        if (prompts.Count > 0)
        {
            builder.AppendLine("Sample questions:");
            foreach (var prompt in prompts)
            {
                builder.AppendLine($"- {prompt}");
            }
        }

        return builder.ToString();
    }
}
