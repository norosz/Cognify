using System.Text.Json;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class KnowledgeStateService(ApplicationDbContext context, IUserContextService userContext) : IKnowledgeStateService
{
    private const double MinScore = 0.0;
    private const double MaxScore = 1.0;

    public async Task ApplyAttemptResultAsync(Attempt attempt, QuestionSet questionSet, IReadOnlyCollection<KnowledgeInteractionInput> interactions)
    {
        var topic = BuildTopic(questionSet);
        var now = DateTime.UtcNow;

        var state = await context.UserKnowledgeStates
            .FirstOrDefaultAsync(s => s.UserId == attempt.UserId && s.Topic == topic);

        if (state == null)
        {
            state = new UserKnowledgeState
            {
                UserId = attempt.UserId,
                Topic = topic,
                SourceNoteId = questionSet.NoteId,
                MasteryScore = 0.5,
                ConfidenceScore = 0.5,
                ForgettingRisk = 0.5,
                LastReviewedAt = now,
                NextReviewAt = now.AddDays(2),
                UpdatedAt = now
            };

            context.UserKnowledgeStates.Add(state);
        }

        var attemptScore = Clamp(attempt.Score / 100.0);
        state.MasteryScore = Clamp((state.MasteryScore * 0.7) + (attemptScore * 0.3));
        state.ConfidenceScore = Clamp((state.ConfidenceScore * 0.7) + (attemptScore * 0.3));
        state.ForgettingRisk = Clamp(1 - state.MasteryScore);
        state.LastReviewedAt = now;
        state.NextReviewAt = CalculateNextReviewAt(state.MasteryScore, now);
        state.UpdatedAt = now;

        var mistakeData = ParseMistakePatterns(state.MistakePatternsJson);
        var incorrectCount = interactions.Count(i => !i.IsCorrect);
        if (incorrectCount > 0)
        {
            mistakeData["incorrectCount"] = mistakeData.GetValueOrDefault("incorrectCount") + incorrectCount;
        }
        state.MistakePatternsJson = JsonSerializer.Serialize(mistakeData);

        foreach (var interaction in interactions)
        {
            var interactionEntity = new LearningInteraction
            {
                UserId = attempt.UserId,
                Topic = topic,
                Type = LearningInteractionType.QuizAnswer,
                AttemptId = attempt.Id,
                QuestionId = interaction.QuestionId,
                UserAnswer = interaction.UserAnswer,
                IsCorrect = interaction.IsCorrect,
                CreatedAt = now
            };

            context.LearningInteractions.Add(interactionEntity);

            var evaluation = new AnswerEvaluation
            {
                LearningInteraction = interactionEntity,
                Score = interaction.IsCorrect ? 1 : 0,
                MaxScore = 1,
                DetectedMistakesJson = interaction.IsCorrect ? null : "[\"IncorrectAnswer\"]",
                CreatedAt = now
            };

            context.AnswerEvaluations.Add(evaluation);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<UserKnowledgeStateDto>> GetMyStatesAsync()
    {
        var userId = userContext.GetCurrentUserId();

        var states = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ForgettingRisk)
            .ThenBy(s => s.NextReviewAt)
            .ToListAsync();

        return states.Select(MapToDto).ToList();
    }

    public async Task<List<ReviewQueueItemDto>> GetReviewQueueAsync(int maxItems = 10)
    {
        var userId = userContext.GetCurrentUserId();
        var now = DateTime.UtcNow;

        var items = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.NextReviewAt != null && s.NextReviewAt <= now)
            .OrderByDescending(s => s.ForgettingRisk)
            .ThenBy(s => s.NextReviewAt)
            .Take(maxItems)
            .ToListAsync();

        return items.Select(s => new ReviewQueueItemDto
        {
            Topic = s.Topic,
            SourceNoteId = s.SourceNoteId,
            MasteryScore = s.MasteryScore,
            ForgettingRisk = s.ForgettingRisk,
            NextReviewAt = s.NextReviewAt
        }).ToList();
    }

    private static UserKnowledgeStateDto MapToDto(UserKnowledgeState state)
    {
        return new UserKnowledgeStateDto
        {
            Id = state.Id,
            Topic = state.Topic,
            SourceNoteId = state.SourceNoteId,
            MasteryScore = state.MasteryScore,
            ConfidenceScore = state.ConfidenceScore,
            ForgettingRisk = state.ForgettingRisk,
            NextReviewAt = state.NextReviewAt,
            LastReviewedAt = state.LastReviewedAt,
            UpdatedAt = state.UpdatedAt
        };
    }

    private static string BuildTopic(QuestionSet questionSet)
    {
        if (!string.IsNullOrWhiteSpace(questionSet.Note?.Title))
        {
            if (!string.IsNullOrWhiteSpace(questionSet.Note.Module?.Title))
            {
                return $"{questionSet.Note.Module.Title} / {questionSet.Note.Title}";
            }

            return questionSet.Note.Title;
        }

        return "General";
    }

    private static DateTime CalculateNextReviewAt(double masteryScore, DateTime now)
    {
        if (masteryScore >= 0.8)
        {
            return now.AddDays(14);
        }

        if (masteryScore >= 0.6)
        {
            return now.AddDays(7);
        }

        if (masteryScore >= 0.4)
        {
            return now.AddDays(3);
        }

        return now.AddDays(1);
    }

    private static Dictionary<string, int> ParseMistakePatterns(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    private static double Clamp(double value)
    {
        return Math.Min(MaxScore, Math.Max(MinScore, value));
    }
}
