using System.Text.Json;
using System.Text.Json.Serialization;
using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Cognify.Server.Models.Ai; // Added for GeneratedQuestion
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class PendingQuizService(ApplicationDbContext db, IServiceProvider serviceProvider) : IPendingQuizService
{
    public async Task<PendingQuiz> CreateAsync(
        Guid userId, Guid noteId, Guid moduleId, string title,
        QuizDifficulty difficulty, int questionType, int questionCount)
    {
        // Resolve ModuleId from Note if not provided
        if (moduleId == Guid.Empty)
        {
            var note = await db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == noteId);
            moduleId = note?.ModuleId ?? Guid.Empty;
        }

        var quiz = new PendingQuiz
        {
            UserId = userId,
            NoteId = noteId,
            ModuleId = moduleId,
            Title = title,
            Difficulty = difficulty,
            QuestionType = questionType,
            QuestionCount = questionCount,
            Status = PendingQuizStatus.Generating
        };

        db.PendingQuizzes.Add(quiz);
        await db.SaveChangesAsync();

        // Trigger AI Generation in the background with proper scope
        _ = Task.Run(async () =>
        {
            using var scope = serviceProvider.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scopedAi = scope.ServiceProvider.GetRequiredService<IAiService>();
            var scopedPending = scope.ServiceProvider.GetRequiredService<IPendingQuizService>();

            try
            {
                var note = await scopedDb.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == noteId);
                if (note == null || string.IsNullOrWhiteSpace(note.Content))
                {
                    await scopedPending.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Failed, errorMessage: "Note content not found.");
                    return;
                }

                var questions = await scopedAi.GenerateQuestionsAsync(
                    note.Content,
                    (QuestionType)questionType,
                    (int)difficulty,
                    questionCount);

                if (questions == null || questions.Count == 0)
                {
                    await scopedPending.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Failed, errorMessage: "AI failed to generate any questions. Please try again with more detailed content.");
                    return;
                }

                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
                var questionsJson = JsonSerializer.Serialize(questions, options);

                // Track actual count
                int actualCount = questions.Count;

                await scopedPending.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Ready, questionsJson: questionsJson, actualQuestionCount: actualCount);
            }
            catch (Exception ex)
            {
                await scopedPending.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Failed, errorMessage: ex.Message);
            }
        });

        return quiz;
    }

    public async Task<List<PendingQuiz>> GetByUserAsync(Guid userId)
    {
        return await db.PendingQuizzes
            .Include(p => p.Note)
            .Include(p => p.Module)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PendingQuiz?> GetByIdAsync(Guid id)
    {
        return await db.PendingQuizzes
            .Include(p => p.Note)
            .Include(p => p.Module)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<QuestionSet> SaveAsQuizAsync(Guid pendingQuizId, Guid userId)
    {
        var pending = await db.PendingQuizzes.FindAsync(pendingQuizId)
            ?? throw new InvalidOperationException("Pending quiz not found.");

        if (pending.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this quiz.");

        if (pending.Status != PendingQuizStatus.Ready)
            throw new InvalidOperationException("Quiz is not ready to be saved.");

        if (string.IsNullOrEmpty(pending.QuestionsJson))
            throw new InvalidOperationException("No questions generated.");

        // Create QuestionSet
        var questionSet = new QuestionSet
        {
            NoteId = pending.NoteId,
            Title = pending.Title,
            Type = (QuestionType)pending.QuestionType
        };

        db.QuestionSets.Add(questionSet);
        await db.SaveChangesAsync();

        // Parse and create Questions
        // Parse and create Questions
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var generatedQuestions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(pending.QuestionsJson, options);

        if (generatedQuestions != null)
        {
            foreach (var gq in generatedQuestions)
            {
                // GeneratedQuestion already has typed QuestionType
                var flattenedAnswer = FlattenCorrectAnswer(gq.CorrectAnswer) ?? (gq.Pairs != null ? string.Join("|", gq.Pairs) : "");

                var question = new Question
                {
                    QuestionSetId = questionSet.Id,
                    Type = gq.Type,
                    Prompt = gq.Text,
                    OptionsJson = JsonSerializer.Serialize(gq.Type == QuestionType.Matching ? (gq.Pairs ?? []) : (gq.Options ?? [])),
                    CorrectAnswerJson = JsonSerializer.Serialize(flattenedAnswer),
                    Explanation = gq.Explanation
                };
                db.Questions.Add(question);
            }
            await db.SaveChangesAsync();
        }

        // Delete pending quiz
        db.PendingQuizzes.Remove(pending);
        await db.SaveChangesAsync();

        return questionSet;
    }

    private static string? FlattenCorrectAnswer(object? obj)
    {
        if (obj == null) return null;
        if (obj is string s) return s;

        if (obj is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString();

            if (element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(item.ToString());
                }
                return string.Join("|", list);
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                var list = new List<string>();
                foreach (var prop in element.EnumerateObject())
                {
                    list.Add($"{prop.Name}:{prop.Value}");
                }
                return string.Join("|", list);
            }

            return element.ToString();
        }

        return obj.ToString();
    }



    public async Task DeleteAsync(Guid id)
    {
        var quiz = await db.PendingQuizzes.FindAsync(id);
        if (quiz != null)
        {
            db.PendingQuizzes.Remove(quiz);
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateStatusAsync(Guid id, PendingQuizStatus status, string? questionsJson = null, string? errorMessage = null, int? actualQuestionCount = null)
    {
        var quiz = await db.PendingQuizzes.FindAsync(id);
        if (quiz != null)
        {
            quiz.Status = status;
            if (questionsJson != null) quiz.QuestionsJson = questionsJson;
            if (errorMessage != null) quiz.ErrorMessage = errorMessage;
            if (actualQuestionCount.HasValue) quiz.ActualQuestionCount = actualQuestionCount.Value;
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountByUserAsync(Guid userId)
    {
        return await db.PendingQuizzes
            .Where(p => p.UserId == userId)
            .CountAsync();
    }
}