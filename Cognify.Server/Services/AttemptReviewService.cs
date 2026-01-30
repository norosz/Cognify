using System.Text.Json;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Attempt;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class AttemptReviewService(ApplicationDbContext context, IUserContextService userContext) : IAttemptReviewService
{
    public async Task<AttemptReviewDto?> GetAttemptReviewAsync(Guid quizId, Guid attemptId)
    {
        var userId = userContext.GetCurrentUserId();

        var attempt = await context.Attempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId && a.QuizId == quizId);

        if (attempt == null)
        {
            return null;
        }

        var quiz = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
        {
            return null;
        }

        var interactions = await context.LearningInteractions
            .AsNoTracking()
            .Where(i => i.AttemptId == attemptId)
            .ToListAsync();

        var interactionIds = interactions.Select(i => i.Id).ToList();
        var evaluations = interactionIds.Count == 0
            ? new List<AnswerEvaluation>()
            : await context.AnswerEvaluations
                .AsNoTracking()
                .Where(e => interactionIds.Contains(e.LearningInteractionId))
                .ToListAsync();

        var items = quiz.Questions.Select(question =>
        {
            var interaction = interactions.FirstOrDefault(i => i.QuestionId == question.Id);
            var evaluation = interaction == null
                ? null
                : evaluations.FirstOrDefault(e => e.LearningInteractionId == interaction.Id);

            return new AttemptReviewItemDto
            {
                QuestionId = question.Id,
                QuestionType = question.Type.ToString(),
                Prompt = question.Prompt,
                UserAnswer = interaction?.UserAnswer,
                CorrectAnswer = TryDeserializeString(question.CorrectAnswerJson),
                IsCorrect = interaction?.IsCorrect,
                Explanation = question.Explanation,
                Feedback = evaluation?.Feedback,
                DetectedMistakes = DeserializeMistakes(evaluation?.DetectedMistakesJson),
                ConfidenceEstimate = evaluation?.ConfidenceEstimate
            };
        }).ToList();

        return new AttemptReviewDto
        {
            AttemptId = attempt.Id,
            QuizId = quiz.Id,
            Score = attempt.Score,
            CreatedAt = attempt.CreatedAt,
            Items = items
        };
    }

    public async Task<AttemptReviewDto?> GetExamAttemptReviewAsync(Guid moduleId, Guid examAttemptId)
    {
        var userId = userContext.GetCurrentUserId();

        var attempt = await context.ExamAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == examAttemptId && a.UserId == userId && a.ModuleId == moduleId);

        if (attempt == null)
        {
            return null;
        }

        var quiz = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == attempt.QuizId);

        if (quiz == null)
        {
            return null;
        }

        var interactions = await context.LearningInteractions
            .AsNoTracking()
            .Where(i => i.ExamAttemptId == examAttemptId)
            .ToListAsync();

        var interactionIds = interactions.Select(i => i.Id).ToList();
        var evaluations = interactionIds.Count == 0
            ? new List<AnswerEvaluation>()
            : await context.AnswerEvaluations
                .AsNoTracking()
                .Where(e => interactionIds.Contains(e.LearningInteractionId))
                .ToListAsync();

        var items = quiz.Questions.Select(question =>
        {
            var interaction = interactions.FirstOrDefault(i => i.QuestionId == question.Id);
            var evaluation = interaction == null
                ? null
                : evaluations.FirstOrDefault(e => e.LearningInteractionId == interaction.Id);

            return new AttemptReviewItemDto
            {
                QuestionId = question.Id,
                QuestionType = question.Type.ToString(),
                Prompt = question.Prompt,
                UserAnswer = interaction?.UserAnswer,
                CorrectAnswer = TryDeserializeString(question.CorrectAnswerJson),
                IsCorrect = interaction?.IsCorrect,
                Explanation = question.Explanation,
                Feedback = evaluation?.Feedback,
                DetectedMistakes = DeserializeMistakes(evaluation?.DetectedMistakesJson),
                ConfidenceEstimate = evaluation?.ConfidenceEstimate
            };
        }).ToList();

        return new AttemptReviewDto
        {
            AttemptId = attempt.Id,
            QuizId = quiz.Id,
            Score = attempt.Score,
            CreatedAt = attempt.CreatedAt,
            Items = items
        };
    }

    private static string? TryDeserializeString(string json)
    {
        try { return JsonSerializer.Deserialize<string>(json); }
        catch { return json; }
    }

    private static List<string> DeserializeMistakes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
