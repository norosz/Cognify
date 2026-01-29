using Cognify.Server.Data;
using Cognify.Server.Dtos;
using Cognify.Server.Dtos.Adaptive;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class AdaptiveQuizService(
    ApplicationDbContext db,
    IUserContextService userContext,
    IKnowledgeStateService knowledgeStateService,
    IPendingQuizService pendingQuizService) : IAdaptiveQuizService
{
    public async Task<AdaptiveQuizResponse> CreateAdaptiveQuizAsync(AdaptiveQuizRequest request)
    {
        var userId = userContext.GetCurrentUserId();

        var target = await SelectTargetAsync(request);
        if (target == null || target.NoteId == null)
        {
            throw new InvalidOperationException("No eligible topics found for adaptive quiz.");
        }

        var note = await db.Notes
            .Include(n => n.Module)
            .FirstOrDefaultAsync(n => n.Id == target.NoteId.Value);

        if (note == null || note.Module == null)
        {
            throw new KeyNotFoundException("Note not found.");
        }

        if (note.Module.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have access to this note.");
        }

        var difficulty = MapDifficulty(target.MasteryScore ?? 0.5);
        var questionType = ParseQuestionType(request.QuestionType);
        var title = BuildTitle(request, note, target.SelectedTopic);

        var pending = await pendingQuizService.CreateAsync(
            userId,
            note.Id,
            note.ModuleId,
            title,
            difficulty,
            questionType,
            request.QuestionCount);

        var response = new AdaptiveQuizResponse(
            MapPendingQuiz(pending, note),
            target.SelectedTopic,
            target.MasteryScore,
            target.ForgettingRisk);

        return response;
    }

    private async Task<AdaptiveTarget?> SelectTargetAsync(AdaptiveQuizRequest request)
    {
        return request.Mode switch
        {
            AdaptiveQuizMode.Review => await SelectFromReviewQueueAsync(request.MaxTopics),
            AdaptiveQuizMode.Weakness => await SelectFromWeakStatesAsync(),
            AdaptiveQuizMode.Note => await SelectFromNoteAsync(request.NoteId),
            _ => null
        };
    }

    private async Task<AdaptiveTarget?> SelectFromReviewQueueAsync(int maxTopics)
    {
        var items = await knowledgeStateService.GetReviewQueueAsync(maxTopics);
        var candidate = items.FirstOrDefault(i => i.SourceNoteId.HasValue);

        return candidate == null
            ? null
            : new AdaptiveTarget(candidate.SourceNoteId, candidate.Topic, candidate.MasteryScore, candidate.ForgettingRisk);
    }

    private async Task<AdaptiveTarget?> SelectFromWeakStatesAsync()
    {
        var states = await knowledgeStateService.GetMyStatesAsync();
        var candidate = states
            .Where(s => s.SourceNoteId.HasValue)
            .OrderByDescending(s => s.ForgettingRisk)
            .ThenBy(s => s.MasteryScore)
            .FirstOrDefault();

        return candidate == null
            ? null
            : new AdaptiveTarget(candidate.SourceNoteId, candidate.Topic, candidate.MasteryScore, candidate.ForgettingRisk);
    }

    private async Task<AdaptiveTarget?> SelectFromNoteAsync(Guid? noteId)
    {
        if (!noteId.HasValue)
        {
            throw new InvalidOperationException("NoteId is required when mode is Note.");
        }

        var states = await knowledgeStateService.GetMyStatesAsync();
        var candidate = states.FirstOrDefault(s => s.SourceNoteId == noteId);

        return new AdaptiveTarget(
            noteId,
            candidate?.Topic,
            candidate?.MasteryScore,
            candidate?.ForgettingRisk);
    }

    private static QuizDifficulty MapDifficulty(double masteryScore)
    {
        if (masteryScore >= 0.75)
        {
            return QuizDifficulty.Advanced;
        }

        if (masteryScore >= 0.5)
        {
            return QuizDifficulty.Intermediate;
        }

        return QuizDifficulty.Beginner;
    }

    private static int ParseQuestionType(string? questionType)
    {
        if (string.IsNullOrWhiteSpace(questionType))
        {
            return (int)QuestionType.MultipleChoice;
        }

        if (Enum.TryParse<QuestionType>(questionType, true, out var parsed))
        {
            return (int)parsed;
        }

        return (int)QuestionType.MultipleChoice;
    }

    private static string BuildTitle(AdaptiveQuizRequest request, Note note, string? topic)
    {
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            return request.Title;
        }

        var modeLabel = request.Mode switch
        {
            AdaptiveQuizMode.Review => "Review Quiz",
            AdaptiveQuizMode.Weakness => "Weakness Quiz",
            AdaptiveQuizMode.Note => "Adaptive Quiz",
            _ => "Adaptive Quiz"
        };

        if (!string.IsNullOrWhiteSpace(topic))
        {
            return $"{modeLabel}: {topic}";
        }

        return $"{modeLabel}: {note.Title}";
    }

    private static PendingQuizDto MapPendingQuiz(PendingQuiz pending, Note note)
    {
        return new PendingQuizDto(
            pending.Id,
            pending.NoteId,
            pending.ModuleId,
            pending.Title,
            note.Title,
            note.Module?.Title ?? "Unknown",
            pending.Difficulty.ToString(),
            GetQuestionTypeName(pending.QuestionType),
            pending.QuestionCount,
            pending.Status.ToString(),
            pending.ErrorMessage,
            pending.CreatedAt
        );
    }

    private static string GetQuestionTypeName(int type) => type switch
    {
        0 => "MultipleChoice",
        1 => "TrueFalse",
        2 => "OpenText",
        3 => "Matching",
        4 => "Ordering",
        5 => "MultipleSelect",
        _ => "Unknown"
    };

    private sealed record AdaptiveTarget(
        Guid? NoteId,
        string? SelectedTopic,
        double? MasteryScore,
        double? ForgettingRisk
    );
}
