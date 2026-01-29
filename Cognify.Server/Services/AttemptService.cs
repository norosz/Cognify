using Cognify.Server.Data;
using Cognify.Server.DTOs;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cognify.Server.Services;

public class AttemptService(ApplicationDbContext context, IUserContextService userContext, IKnowledgeStateService knowledgeStateService) : IAttemptService
{
    public async Task<AttemptDto> SubmitAttemptAsync(SubmitAttemptDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        
        var questionSet = await context.QuestionSets
            .Include(qs => qs.Questions)
            .Include(qs => qs.Note)
            .ThenInclude(n => n.Module)
            .FirstOrDefaultAsync(qs => qs.Id == dto.QuestionSetId);

        if (questionSet == null)
             throw new KeyNotFoundException("Question set not found.");

        double correctCount = 0;
        int totalQuestions = questionSet.Questions.Count;

        var interactions = new List<KnowledgeInteractionInput>();

        foreach (var question in questionSet.Questions)
        {
            var qIdStr = question.Id.ToString();
            // Case insensitive key lookup could be nice, but GUIDs are usually standard.
            // We'll iterate keys to find match if needed, but dictionary lookup is better.
            // Assuming frontend sends exact GUID string.
            
            // Try explicit match first
            string? userAnswer = null;
            if (dto.Answers.TryGetValue(qIdStr, out var ans))
            {
                userAnswer = ans;
            }
            else
            {
                // Fallback: Check case-insensitive keys (GUIDs might differ in casing)
                var match = dto.Answers.FirstOrDefault(k => string.Equals(k.Key, qIdStr, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key))
                {
                    userAnswer = match.Value;
                }
            }

            var isCorrect = false;

            if (userAnswer != null)
            {
                var correctAnswer = JsonSerializer.Deserialize<string>(question.CorrectAnswerJson);
                if (string.Equals(userAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    correctCount++;
                    isCorrect = true;
                }
            }

            interactions.Add(new KnowledgeInteractionInput
            {
                QuestionId = question.Id,
                UserAnswer = userAnswer,
                IsCorrect = isCorrect
            });
        }

        double score = totalQuestions > 0 ? (correctCount / totalQuestions) * 100 : 0;

        var attempt = new Attempt
        {
            UserId = userId,
            QuestionSetId = dto.QuestionSetId,
            AnswersJson = JsonSerializer.Serialize(dto.Answers),
            Score = score,
            TimeSpentSeconds = dto.TimeSpentSeconds,
            Difficulty = dto.Difficulty ?? questionSet.Difficulty.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        context.Attempts.Add(attempt);
        await context.SaveChangesAsync();

        await knowledgeStateService.ApplyAttemptResultAsync(attempt, questionSet, interactions);

        return MapToDto(attempt);
    }

    public async Task<List<AttemptDto>> GetAttemptsAsync(Guid questionSetId)
    {
        var userId = userContext.GetCurrentUserId();
        
        var attempts = await context.Attempts
            .AsNoTracking()
            .Where(a => a.QuestionSetId == questionSetId && a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return attempts.Select(MapToDto).ToList();
    }
    
    public async Task<AttemptDto?> GetAttemptByIdAsync(Guid id)
    {
         var userId = userContext.GetCurrentUserId();
         var attempt = await context.Attempts.AsNoTracking()
             .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
             
         return attempt == null ? null : MapToDto(attempt);
    }

    private static AttemptDto MapToDto(Attempt attempt)
    {
        return new AttemptDto
        {
            Id = attempt.Id,
            QuestionSetId = attempt.QuestionSetId,
            UserId = attempt.UserId,
            Score = attempt.Score,
            Answers = TryDeserializeAttempts(attempt.AnswersJson),
            TimeSpentSeconds = attempt.TimeSpentSeconds,
            Difficulty = attempt.Difficulty,
            CreatedAt = attempt.CreatedAt
        };
    }

    private static Dictionary<string, string> TryDeserializeAttempts(string json)
    {
        try 
        { 
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []; 
        }
        catch 
        { 
            return []; 
        }
    }
}
