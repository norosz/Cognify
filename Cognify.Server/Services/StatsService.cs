using Cognify.Server.Data;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class StatsService(ApplicationDbContext context, IUserContextService userContext) : IStatsService
{
    public async Task<ModuleStatsDto?> GetModuleStatsAsync(Guid moduleId)
    {
        var userId = userContext.GetCurrentUserId();

        var module = await context.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);

        if (module == null)
        {
            return null;
        }

        var noteIds = await context.Notes
            .AsNoTracking()
            .Where(n => n.ModuleId == moduleId)
            .Select(n => n.Id)
            .ToListAsync();

        var practiceAttempts = await context.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(context.Quizzes.AsNoTracking(), a => a.QuizId, q => q.Id, (a, q) => new { a, q })
            .Join(context.Notes.AsNoTracking(), aq => aq.q.NoteId, n => n.Id, (aq, n) => new { aq.a, n })
            .Where(x => x.n.ModuleId == moduleId)
            .Select(x => x.a)
            .ToListAsync();

        var examAttempts = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.ModuleId == moduleId)
            .ToListAsync();

        var knowledgeStates = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.SourceNoteId.HasValue && noteIds.Contains(s.SourceNoteId.Value))
            .ToListAsync();

        var weakTopics = knowledgeStates
            .Select(s => new ModuleWeakTopicDto
            {
                Topic = s.Topic,
                MasteryScore = s.MasteryScore,
                ForgettingRisk = s.ForgettingRisk,
                NextReviewAt = s.NextReviewAt
            })
            .OrderByDescending(s => (1 - s.MasteryScore) + s.ForgettingRisk)
            .Take(5)
            .ToList();

        var totalDocuments = await context.Documents.AsNoTracking().CountAsync(d => d.ModuleId == moduleId);
        var totalNotes = noteIds.Count;
        var totalQuizzes = await context.Quizzes.AsNoTracking().CountAsync(q => noteIds.Contains(q.NoteId));

        return new ModuleStatsDto
        {
            ModuleId = moduleId,
            TotalDocuments = totalDocuments,
            TotalNotes = totalNotes,
            TotalQuizzes = totalQuizzes,
            AverageMastery = knowledgeStates.Count > 0 ? knowledgeStates.Average(s => s.MasteryScore) : 0,
            AverageForgettingRisk = knowledgeStates.Count > 0 ? knowledgeStates.Average(s => s.ForgettingRisk) : 0,
            PracticeAttemptCount = practiceAttempts.Count,
            PracticeAverageScore = practiceAttempts.Count > 0 ? practiceAttempts.Average(a => a.Score) : 0,
            PracticeBestScore = practiceAttempts.Count > 0 ? practiceAttempts.Max(a => a.Score) : 0,
            LastPracticeAttemptAt = practiceAttempts.Count > 0 ? practiceAttempts.Max(a => a.CreatedAt) : null,
            ExamAttemptCount = examAttempts.Count,
            ExamAverageScore = examAttempts.Count > 0 ? examAttempts.Average(a => a.Score) : 0,
            ExamBestScore = examAttempts.Count > 0 ? examAttempts.Max(a => a.Score) : 0,
            LastExamAttemptAt = examAttempts.Count > 0 ? examAttempts.Max(a => a.CreatedAt) : null,
            WeakTopics = weakTopics
        };
    }

    public async Task<QuizStatsDto?> GetQuizStatsAsync(Guid quizId)
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

        var attempts = await context.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.QuizId == quizId)
            .ToListAsync();

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var interactions = attemptIds.Count == 0
            ? new List<LearningInteraction>()
            : await context.LearningInteractions
                .AsNoTracking()
                .Where(i => i.AttemptId.HasValue && attemptIds.Contains(i.AttemptId.Value))
                .ToListAsync();

        var accuracyRate = interactions.Count(i => i.IsCorrect.HasValue) > 0
            ? interactions.Where(i => i.IsCorrect.HasValue).Average(i => i.IsCorrect == true ? 1 : 0)
            : 0;

        return new QuizStatsDto
        {
            QuizId = quizId,
            QuestionCount = quiz.Questions.Count,
            AttemptCount = attempts.Count,
            AverageScore = attempts.Count > 0 ? attempts.Average(a => a.Score) : 0,
            BestScore = attempts.Count > 0 ? attempts.Max(a => a.Score) : 0,
            LastAttemptAt = attempts.Count > 0 ? attempts.Max(a => a.CreatedAt) : null,
            AccuracyRate = accuracyRate
        };
    }
}
