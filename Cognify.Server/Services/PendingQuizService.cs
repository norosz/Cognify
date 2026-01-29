using System.Text.Json;
using System.Text.Json.Serialization;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai; // Added for GeneratedQuestion
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class PendingQuizService(ApplicationDbContext db, IAgentRunService agentRunService) : IPendingQuizService
{
    public async Task<PendingQuiz> CreateAsync(
        Guid userId, Guid noteId, Guid moduleId, string title,
        QuizDifficulty difficulty, int questionType, int questionCount)
    {
        var existing = await db.PendingQuizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.NoteId == noteId &&
                p.QuestionType == questionType &&
                p.QuestionCount == questionCount &&
                p.Difficulty == difficulty &&
                p.Status != PendingQuizStatus.Failed);

        if (existing != null)
        {
            return existing;
        }

        var inputHash = AgentRunService.ComputeHash($"quiz:{AgentContractVersions.V2}:{userId}:{noteId}:{questionType}:{questionCount}:{difficulty}");
        var run = await agentRunService.CreateAsync(userId, AgentRunType.QuizGeneration, inputHash, promptVersion: "quizgen-v2");

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
            AgentRunId = run.Id,
            Title = title,
            Difficulty = difficulty,
            QuestionType = questionType,
            QuestionCount = questionCount,
            Status = PendingQuizStatus.Generating
        };

        db.PendingQuizzes.Add(quiz);
        await db.SaveChangesAsync();

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
        var quizRubric = ParseQuizRubric(pending.QuestionsJson);
        var questionSet = new QuestionSet
        {
            NoteId = pending.NoteId,
            Title = pending.Title,
            Difficulty = pending.Difficulty,
            Type = (QuestionType)pending.QuestionType,
            RubricJson = quizRubric
        };

        db.QuestionSets.Add(questionSet);
        await db.SaveChangesAsync();

        // Parse and create Questions
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var generatedQuestions = ParseGeneratedQuestions(pending.QuestionsJson, options);

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

    private static List<GeneratedQuestion> ParseGeneratedQuestions(string json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<GeneratedQuestion>>(root.GetRawText(), options) ?? [];
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "questions", StringComparison.OrdinalIgnoreCase)
                        && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        return JsonSerializer.Deserialize<List<GeneratedQuestion>>(prop.Value.GetRawText(), options) ?? [];
                    }
                }
            }
        }
        catch
        {
            return [];
        }

        return [];
    }

    private static string? ParseQuizRubric(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "quizRubric", StringComparison.OrdinalIgnoreCase)
                    && prop.Value.ValueKind != JsonValueKind.Null)
                {
                    return prop.Value.ToString();
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
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