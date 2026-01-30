using Cognify.Server.Data;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class FinalExamService(
    ApplicationDbContext context,
    IUserContextService userContext,
    IPendingQuizService pendingQuizService) : IFinalExamService
{
    private const string FinalExamNoteTitle = "Final Exam";
    private const string FinalExamNoteMarker = "<!-- cognify:final-exam -->";

    public async Task<FinalExamDto?> GetFinalExamAsync(Guid moduleId)
    {
        var module = await GetOwnedModuleAsync(moduleId);
        if (module == null)
        {
            return null;
        }

        if (module.CurrentFinalExamQuizId == null)
        {
            return new FinalExamDto
            {
                ModuleId = module.Id,
                CurrentQuizId = null,
                CurrentQuizTitle = null,
                CurrentQuizCreatedAt = null
            };
        }

        var quiz = await context.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == module.CurrentFinalExamQuizId);

        return new FinalExamDto
        {
            ModuleId = module.Id,
            CurrentQuizId = quiz?.Id,
            CurrentQuizTitle = quiz?.Title,
            CurrentQuizCreatedAt = quiz?.CreatedAt
        };
    }

    public async Task<PendingQuiz> RegenerateFinalExamAsync(Guid moduleId, RegenerateFinalExamRequest request)
    {
        var module = await GetOwnedModuleAsync(moduleId);
        if (module == null)
        {
            throw new UnauthorizedAccessException("Module not found or not owned by user.");
        }

        var selectedNotesCount = await GetSelectedNotesCountAsync(module.Id);
        if (selectedNotesCount == 0)
        {
            throw new InvalidOperationException("No notes are selected for the final exam.");
        }

        await ClearExistingPendingQuizzesAsync(module.Id);

        if (!Enum.TryParse<QuizDifficulty>(request.Difficulty, true, out var difficulty))
        {
            difficulty = QuizDifficulty.Intermediate;
        }

        var questionType = MapQuestionType(request.QuestionType);

        return await pendingQuizService.CreateAsync(
            userContext.GetCurrentUserId(),
            noteId: null,
            module.Id,
            request.Title,
            difficulty,
            questionType,
            request.QuestionCount);
    }

    public async Task<int> IncludeAllNotesForFinalExamAsync(Guid moduleId)
    {
        var module = await GetOwnedModuleAsync(moduleId);
        if (module == null)
        {
            throw new UnauthorizedAccessException("Module not found or not owned by user.");
        }

        var notes = await context.Notes
            .Where(n => n.ModuleId == module.Id)
            .ToListAsync();

        var updatedCount = 0;

        foreach (var note in notes)
        {
            if (IsFinalExamMarkerNote(note))
            {
                continue;
            }

            if (!note.IncludeInFinalExam)
            {
                note.IncludeInFinalExam = true;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync();
        }

        return updatedCount;
    }

    public async Task<FinalExamSaveResultDto> SaveFinalExamAsync(Guid moduleId, Guid pendingQuizId)
    {
        var module = await GetOwnedModuleAsync(moduleId);
        if (module == null)
        {
            throw new UnauthorizedAccessException("Module not found or not owned by user.");
        }

        var quiz = await pendingQuizService.SaveAsQuizAsync(pendingQuizId, userContext.GetCurrentUserId());

        module.CurrentFinalExamQuizId = quiz.Id;
        await context.SaveChangesAsync();

        return new FinalExamSaveResultDto { QuizId = quiz.Id };
    }

    private async Task<Module?> GetOwnedModuleAsync(Guid moduleId)
    {
        var userId = userContext.GetCurrentUserId();
        return await context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);
    }

    private async Task ClearExistingPendingQuizzesAsync(Guid moduleId)
    {
        var userId = userContext.GetCurrentUserId();
        var pending = await context.PendingQuizzes
            .Where(p => p.UserId == userId && p.ModuleId == moduleId && p.NoteId == null && p.Status != PendingQuizStatus.Failed)
            .ToListAsync();

        if (pending.Count == 0)
        {
            return;
        }

        context.PendingQuizzes.RemoveRange(pending);
        await context.SaveChangesAsync();
    }

    private async Task<int> GetSelectedNotesCountAsync(Guid moduleId)
    {
        var notes = await context.Notes
            .AsNoTracking()
            .Where(n => n.ModuleId == moduleId && n.IncludeInFinalExam)
            .ToListAsync();

        return notes.Count(n => !IsFinalExamMarkerNote(n));
    }

    private static bool IsFinalExamMarkerNote(Note note)
    {
        return note.Title == FinalExamNoteTitle &&
               !string.IsNullOrWhiteSpace(note.Content) &&
               note.Content.Contains(FinalExamNoteMarker);
    }

    private static int MapQuestionType(string questionType)
    {
        return questionType.ToLowerInvariant() switch
        {
            "mixed" => -1,
            "multiplechoice" => 0,
            "truefalse" => 1,
            "opentext" => 2,
            "matching" => 3,
            "ordering" => 4,
            "multipleselect" => 5,
            _ => 0
        };
    }
}
