using Cognify.Server.Data;
using Cognify.Server.DTOs;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cognify.Server.Services;

public class QuestionService(ApplicationDbContext context, IUserContextService userContext) : IQuestionService
{
    public async Task<QuestionSetDto> CreateAsync(CreateQuestionSetDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        
        // Verify note ownership
        var note = await context.Notes.Include(n => n.Module).FirstOrDefaultAsync(n => n.Id == dto.NoteId);
        if (note == null || note.Module == null || note.Module.OwnerUserId != userId)
        {
             throw new UnauthorizedAccessException("User does not own the note.");
        }

        var questionSet = new QuestionSet
        {
            NoteId = dto.NoteId,
            Title = dto.Title,
            Type = Enum.TryParse<QuestionType>(dto.Questions.FirstOrDefault()?.Type ?? "MultipleChoice", true, out var qt) ? qt : QuestionType.MultipleChoice,
            CreatedAt = DateTime.UtcNow
        };

        var questions = dto.Questions.Select(q => new Question
        {
            QuestionSetId = questionSet.Id,
            Prompt = q.Prompt,
            Type = Enum.TryParse<QuestionType>(q.Type, true, out var t) ? t : QuestionType.MultipleChoice,
            OptionsJson = JsonSerializer.Serialize(q.Options),
            CorrectAnswerJson = JsonSerializer.Serialize(q.CorrectAnswer),
            Explanation = q.Explanation
        }).ToList();

        questionSet.Questions = questions;

        context.QuestionSets.Add(questionSet);
        await context.SaveChangesAsync();

        return MapToDto(questionSet);
    }

    public async Task<QuestionSetDto?> GetByIdAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        
        var qs = await context.QuestionSets
            .AsNoTracking()
            .Include(qs => qs.Questions)
            .Include(qs => qs.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(qs => qs.Id == id);
            
        if (qs == null || qs.Note == null || qs.Note.Module == null || qs.Note.Module.OwnerUserId != userId)
            return null;

        return MapToDto(qs);
    }

    public async Task<List<QuestionSetDto>> GetByNoteIdAsync(Guid noteId)
    {
         var userId = userContext.GetCurrentUserId();
         
         var list = await context.QuestionSets
            .AsNoTracking()
            .Include(qs => qs.Questions)
            .Include(qs => qs.Note)
            .ThenInclude(n => n!.Module)
            .Where(qs => qs.NoteId == noteId)
            .OrderByDescending(qs => qs.CreatedAt)
            .ToListAsync();
            
         // Filter by ownership
         return list.Where(qs => qs.Note?.Module?.OwnerUserId == userId)
                    .Select(MapToDto)
                    .ToList();
    }
    
    public async Task<bool> DeleteAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
         var qs = await context.QuestionSets
            .Include(qs => qs.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(qs => qs.Id == id);
            
        if (qs == null || qs.Note?.Module?.OwnerUserId != userId)
            return false;
            
        context.QuestionSets.Remove(qs);
        await context.SaveChangesAsync();
        return true;
    }

    private static QuestionSetDto MapToDto(QuestionSet qs)
    {
        return new QuestionSetDto
        {
            Id = qs.Id,
            NoteId = qs.NoteId,
            Title = qs.Title,
            Type = qs.Type.ToString(),
            CreatedAt = qs.CreatedAt,
            Questions = qs.Questions?.Select(q => new QuestionDto
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
