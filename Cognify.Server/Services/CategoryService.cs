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
        return await aiService.SuggestCategoriesAsync(contextText, maxSuggestions);
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

        var contextText = BuildQuizContext(
            quiz.Title,
            quiz.Difficulty.ToString(),
            quiz.Type.ToString(),
            quiz.Questions.Select(q => q.Prompt).Take(3).ToList());

        return await aiService.SuggestCategoriesAsync(contextText, maxSuggestions);
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
        return true;
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
