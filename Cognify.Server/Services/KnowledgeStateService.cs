using System.Text.Json;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class KnowledgeStateService(
    ApplicationDbContext context,
    IUserContextService userContext,
    IDecayPredictionService decayService,
    IMistakeAnalysisService mistakeService) : IKnowledgeStateService
{
    private const double MinScore = 0.0;
    private const double MaxScore = 1.0;

    public async Task ApplyAttemptResultAsync(Attempt attempt, Quiz quiz, IReadOnlyCollection<KnowledgeInteractionInput> interactions)
    {
        var topic = BuildTopic(quiz);
        var now = DateTime.UtcNow;

        var state = await context.UserKnowledgeStates
            .FirstOrDefaultAsync(s => s.UserId == attempt.UserId && s.Topic == topic);

        if (state == null)
        {
            state = new UserKnowledgeState
            {
                UserId = attempt.UserId,
                Topic = topic,
                SourceNoteId = quiz.NoteId,
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

        state.ForgettingRisk = decayService.CalculateForgettingRisk(state.MasteryScore, state.LastReviewedAt, now);
        state.LastReviewedAt = now;
        state.NextReviewAt = decayService.CalculateNextReviewAt(state.MasteryScore, now);
        state.UpdatedAt = now;

        var mistakeData = mistakeService.UpdateMistakePatterns(state.MistakePatternsJson, interactions);
        state.MistakePatternsJson = mistakeService.SerializeMistakePatterns(mistakeData);

        var mistakeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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

            var mistakes = interaction.DetectedMistakes?.ToList() ?? mistakeService.DetectMistakes(interaction);
            foreach (var mistake in mistakes)
            {
                mistakeCounts[mistake] = mistakeCounts.GetValueOrDefault(mistake) + 1;
            }

            var evaluation = new AnswerEvaluation
            {
                LearningInteraction = interactionEntity,
                Score = interaction.Score > 0 ? interaction.Score : (interaction.IsCorrect ? 1 : 0),
                MaxScore = interaction.MaxScore > 0 ? interaction.MaxScore : 1,
                Feedback = interaction.Feedback,
                ConfidenceEstimate = interaction.ConfidenceEstimate,
                DetectedMistakesJson = mistakes.Count == 0 ? null : JsonSerializer.Serialize(mistakes),
                CreatedAt = now
            };

            context.AnswerEvaluations.Add(evaluation);
        }

        await UpsertStructuredMistakesAsync(attempt.UserId, topic, quiz.NoteId, mistakeCounts, now);

        await context.SaveChangesAsync();
    }

    public async Task RecordExamAttemptAsync(ExamAttempt attempt, Quiz quiz, IReadOnlyCollection<KnowledgeInteractionInput> interactions)
    {
        var topic = BuildTopic(quiz);
        var now = DateTime.UtcNow;

        foreach (var interaction in interactions)
        {
            var interactionEntity = new LearningInteraction
            {
                UserId = attempt.UserId,
                Topic = topic,
                Type = LearningInteractionType.QuizAnswer,
                ExamAttemptId = attempt.Id,
                QuestionId = interaction.QuestionId,
                UserAnswer = interaction.UserAnswer,
                IsCorrect = interaction.IsCorrect,
                CreatedAt = now
            };

            context.LearningInteractions.Add(interactionEntity);

            var mistakes = interaction.DetectedMistakes?.ToList() ?? mistakeService.DetectMistakes(interaction);

            var evaluation = new AnswerEvaluation
            {
                LearningInteraction = interactionEntity,
                Score = interaction.Score > 0 ? interaction.Score : (interaction.IsCorrect ? 1 : 0),
                MaxScore = interaction.MaxScore > 0 ? interaction.MaxScore : 1,
                Feedback = interaction.Feedback,
                ConfidenceEstimate = interaction.ConfidenceEstimate,
                DetectedMistakesJson = mistakes.Count == 0 ? null : JsonSerializer.Serialize(mistakes),
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

    public async Task<List<ReviewQueueItemDto>> GetReviewQueueAsync(int maxItems = 10, bool includeExams = false)
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

    private static string BuildTopic(Quiz quiz)
    {
        if (!string.IsNullOrWhiteSpace(quiz.Note?.Title))
        {
            if (!string.IsNullOrWhiteSpace(quiz.Note.Module?.Title))
            {
                return $"{quiz.Note.Module.Title} / {quiz.Note.Title}";
            }

            return quiz.Note.Title;
        }

        return "General";
    }

    private static double Clamp(double value)
    {
        return Math.Min(MaxScore, Math.Max(MinScore, value));
    }

    private async Task UpsertStructuredMistakesAsync(
        Guid userId,
        string topic,
        Guid? sourceNoteId,
        Dictionary<string, int> mistakeCounts,
        DateTime now)
    {
        if (mistakeCounts.Count == 0)
        {
            return;
        }

        var categories = mistakeCounts.Keys.ToList();
        var existing = await context.UserMistakePatterns
            .Where(p => p.UserId == userId && p.Topic == topic && categories.Contains(p.Category))
            .ToListAsync();

        foreach (var (category, count) in mistakeCounts)
        {
            var current = existing.FirstOrDefault(p => p.Category == category);
            if (current == null)
            {
                context.UserMistakePatterns.Add(new UserMistakePattern
                {
                    UserId = userId,
                    Topic = topic,
                    SourceNoteId = sourceNoteId,
                    Category = category,
                    Count = count,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    UpdatedAt = now
                });
                continue;
            }

            current.Count += count;
            current.LastSeenAt = now;
            current.UpdatedAt = now;
            if (current.SourceNoteId == null && sourceNoteId.HasValue)
            {
                current.SourceNoteId = sourceNoteId;
            }
        }
    }
}
