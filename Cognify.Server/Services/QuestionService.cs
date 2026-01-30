using Cognify.Server.Data;
using Cognify.Server.DTOs;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cognify.Server.Services;

public class QuizService(ApplicationDbContext context, IUserContextService userContext) : IQuizService
{
    public async Task<QuizDto> CreateAsync(CreateQuizDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        
        // Verify note ownership
        var note = await context.Notes.Include(n => n.Module).FirstOrDefaultAsync(n => n.Id == dto.NoteId);
        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
             throw new UnauthorizedAccessException("User does not own the note.");
        }

        var quiz = new Quiz
        {
            NoteId = dto.NoteId,
            Title = dto.Title,
            Difficulty = Enum.TryParse<QuizDifficulty>(dto.Difficulty ?? "", true, out var diff) ? diff : QuizDifficulty.Intermediate,
            Type = Enum.TryParse<QuestionType>(dto.Questions.FirstOrDefault()?.Type ?? "MultipleChoice", true, out var qt) ? qt : QuestionType.MultipleChoice,
            RubricJson = string.IsNullOrWhiteSpace(dto.QuizRubric) ? null : dto.QuizRubric,
            CreatedAt = DateTime.UtcNow
        };

        var questions = dto.Questions.Select(q => new QuizQuestion
        {
            QuizId = quiz.Id,
            Prompt = q.Prompt,
            Type = Enum.TryParse<QuestionType>(q.Type, true, out var t) ? t : QuestionType.MultipleChoice,
            OptionsJson = JsonSerializer.Serialize(q.Options),
            CorrectAnswerJson = JsonSerializer.Serialize(q.CorrectAnswer),
            Explanation = q.Explanation
        }).ToList();

        quiz.Questions = questions;

        context.Quizzes.Add(quiz);
        await context.SaveChangesAsync();

        return MapToDto(quiz);
    }

    public async Task<QuizDto?> GetByIdAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        
        var quiz = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(q => q.Id == id);
            
        if (quiz == null || quiz.Note == null || quiz.Note.Module == null || quiz.Note.Module.OwnerUserId != userId)
            return null;

        return MapToDto(quiz);
    }

    public async Task<List<QuizDto>> GetByNoteIdAsync(Guid noteId)
    {
         var userId = userContext.GetCurrentUserId();
         
         var list = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .Where(q => q.NoteId == noteId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
            
         // Filter by ownership
         return list.Where(q => q.Note?.Module?.OwnerUserId == userId)
                    .Select(MapToDto)
                    .ToList();
    }
    
    public async Task<bool> DeleteAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
         var quiz = await context.Quizzes
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(q => q.Id == id);
            
        if (quiz == null || quiz.Note?.Module?.OwnerUserId != userId)
            return false;
            
        context.Quizzes.Remove(quiz);
        await context.SaveChangesAsync();
        return true;
    }

    private static QuizDto MapToDto(Quiz quiz)
    {
        return new QuizDto
        {
            Id = quiz.Id,
            NoteId = quiz.NoteId,
            Title = quiz.Title,
            QuestionCount = quiz.Questions?.Count ?? 0,
            Type = quiz.Type.ToString(),
            Difficulty = quiz.Difficulty.ToString(),
            QuizRubric = quiz.RubricJson,
            CategoryLabel = quiz.CategoryLabel,
            CategorySource = quiz.CategorySource,
            CreatedAt = quiz.CreatedAt,
            Questions = quiz.Questions?.Select(q => new QuizQuestionDto
            {
                Id = q.Id,
                Prompt = q.Prompt,
                Type = q.Type.ToString(),
                Options = TryDeserializeList(q.OptionsJson),
                CorrectAnswer = TryDeserializeString(q.CorrectAnswerJson),
                Explanation = q.Explanation ?? string.Empty
            }).ToList() ?? []
        };
    }

    private static List<string> TryDeserializeList(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }
    
    private static string TryDeserializeString(string json)
    {
         try { return JsonSerializer.Deserialize<string>(json) ?? string.Empty; }
         catch { return json; } // If not valid JSON, returns raw string? But we serialized it.
    }
}
