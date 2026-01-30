using System.Text.Json;
using System.Text.Json.Serialization;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai; // Added for GeneratedQuestion
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class PendingQuizService(ApplicationDbContext db, IAgentRunService agentRunService, IAiService aiService) : IPendingQuizService
{
    public async Task<PendingQuiz> CreateAsync(
        Guid userId, Guid? noteId, Guid moduleId, string title,
        QuizDifficulty difficulty, int questionType, int questionCount)
    {
        var existingQuery = db.PendingQuizzes
            .AsNoTracking()
            .Where(p =>
                p.UserId == userId &&
                p.QuestionType == questionType &&
                p.QuestionCount == questionCount &&
                p.Difficulty == difficulty &&
                p.Status != PendingQuizStatus.Failed);

        if (noteId.HasValue)
        {
            existingQuery = existingQuery.Where(p => p.NoteId == noteId);
        }
        else
        {
            existingQuery = existingQuery.Where(p => p.NoteId == null && p.ModuleId == moduleId);
        }

        var existing = await existingQuery.FirstOrDefaultAsync();

        if (existing != null)
        {
            return existing;
        }

        var noteKey = noteId?.ToString() ?? $"module:{moduleId}";
        var inputHash = AgentRunService.ComputeHash($"quiz:{AgentContractVersions.V2}:{userId}:{noteKey}:{questionType}:{questionCount}:{difficulty}");
        var run = await agentRunService.CreateAsync(userId, AgentRunType.QuizGeneration, inputHash, promptVersion: "quizgen-v2");

        // Resolve ModuleId from Note if not provided
        if (moduleId == Guid.Empty)
        {
            if (noteId.HasValue)
            {
                var note = await db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == noteId);
                moduleId = note?.ModuleId ?? Guid.Empty;
            }
        }

        if (moduleId == Guid.Empty)
        {
            throw new InvalidOperationException("ModuleId is required for pending quizzes without a note.");
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

    public async Task<Quiz> SaveAsQuizAsync(Guid pendingQuizId, Guid userId)
    {
        var pending = await db.PendingQuizzes.FindAsync(pendingQuizId)
            ?? throw new InvalidOperationException("Pending quiz not found.");

        if (pending.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this quiz.");

        if (pending.Status != PendingQuizStatus.Ready)
            throw new InvalidOperationException("Quiz is not ready to be saved.");

        if (string.IsNullOrEmpty(pending.QuestionsJson))
            throw new InvalidOperationException("No questions generated.");

        // Parse and repair Questions before save
        var quizRubric = ParseQuizRubric(pending.QuestionsJson);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var generatedQuestions = ParseGeneratedQuestions(pending.QuestionsJson, options);

        var finalQuestions = generatedQuestions ?? [];
        var finalRubric = quizRubric;

        if (finalQuestions.Count > 0)
        {
            var repaired = await RepairGeneratedQuestionsAsync(userId, pending, finalQuestions, finalRubric);
            finalQuestions = repaired.Questions;
            finalRubric = repaired.QuizRubric;
        }

        // Create Quiz
        var quiz = new Quiz
        {
            NoteId = pending.NoteId,
            Title = pending.Title,
            Difficulty = pending.Difficulty,
            Type = (QuestionType)pending.QuestionType,
            RubricJson = finalRubric
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        if (finalQuestions.Count > 0)
        {
            foreach (var gq in finalQuestions)
            {
                // GeneratedQuestion already has typed QuestionType
                var flattenedAnswer = FlattenCorrectAnswer(gq.CorrectAnswer) ?? (gq.Pairs != null ? string.Join("|", gq.Pairs) : "");

                var question = new QuizQuestion
                {
                    QuizId = quiz.Id,
                    Type = gq.Type,
                    Prompt = gq.Text,
                    OptionsJson = JsonSerializer.Serialize(gq.Type == QuestionType.Matching ? (gq.Pairs ?? []) : (gq.Options ?? [])),
                    CorrectAnswerJson = JsonSerializer.Serialize(flattenedAnswer),
                    Explanation = gq.Explanation
                };
                db.QuizQuestions.Add(question);
            }
            await db.SaveChangesAsync();
        }

        await ApplyAiCategorySuggestionAsync(quiz, pending.ModuleId, userId, finalQuestions);

        // Delete pending quiz
        try
        {
            if (db.Entry(pending).State == EntityState.Detached)
            {
                var existing = await db.PendingQuizzes.FindAsync(pendingQuizId);
                if (existing != null)
                {
                    db.PendingQuizzes.Remove(existing);
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                db.PendingQuizzes.Remove(pending);
                await db.SaveChangesAsync();
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // Pending quiz was already deleted by another process; safe to ignore.
        }

        return quiz;
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

    private async Task<(List<GeneratedQuestion> Questions, string? QuizRubric)> RepairGeneratedQuestionsAsync(
        Guid userId,
        PendingQuiz pending,
        List<GeneratedQuestion> questions,
        string? quizRubric)
    {
        var inputHash = AgentRunService.ComputeHash($"quizrepair:{AgentContractVersions.V2}:{userId}:{pending.Id}:{pending.QuestionsJson ?? string.Empty}");
        var run = await agentRunService.CreateAsync(userId, AgentRunType.QuizRepair, inputHash, correlationId: pending.Id.ToString(), promptVersion: "quizrepair-v1");

        try
        {
            var response = await aiService.RepairQuizAsync(new QuizRepairContractRequest(
                AgentContractVersions.V2,
                questions,
                quizRubric));

            var output = JsonSerializer.Serialize(new
            {
                response.ContractVersion,
                response.Questions,
                response.QuizRubric
            });

            await agentRunService.MarkCompletedAsync(run.Id, output);

            if (response.Questions.Count == 0)
            {
                return (questions, quizRubric);
            }

            return (response.Questions, response.QuizRubric ?? quizRubric);
        }
        catch (Exception ex)
        {
            await agentRunService.MarkFailedAsync(run.Id, ex.Message);
            return (questions, quizRubric);
        }
    }

    private async Task ApplyAiCategorySuggestionAsync(Quiz quiz, Guid moduleId, Guid userId, List<GeneratedQuestion> questions)
    {
        if (!string.IsNullOrWhiteSpace(quiz.CategoryLabel))
        {
            return;
        }

        var module = await db.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId);
        var contextText = BuildQuizCategoryContext(quiz, module, questions);
        var suggestions = await aiService.SuggestCategoriesAsync(contextText, 3);
        if (suggestions.Suggestions.Count == 0)
        {
            return;
        }

        var moduleCategory = module?.CategoryLabel?.Trim();
        var selected = suggestions.Suggestions
            .Select(s => s.Label?.Trim())
            .FirstOrDefault(label => !string.IsNullOrWhiteSpace(label)
                && (string.IsNullOrWhiteSpace(moduleCategory)
                    || !string.Equals(label, moduleCategory, StringComparison.OrdinalIgnoreCase)));

        selected ??= suggestions.Suggestions.FirstOrDefault()?.Label?.Trim();
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        quiz.CategoryLabel = selected;
        quiz.CategorySource = "AI";

        db.CategorySuggestionBatches.Add(new CategorySuggestionBatch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuizId = quiz.Id,
            Source = "AI",
            CreatedAt = DateTime.UtcNow,
            Items = suggestions.Suggestions.Select(item => new CategorySuggestionItem
            {
                Id = Guid.NewGuid(),
                Label = item.Label,
                Confidence = item.Confidence,
                Rationale = item.Rationale
            }).ToList()
        });

        await db.SaveChangesAsync();
    }

    private static string BuildQuizCategoryContext(Quiz quiz, Module? module, List<GeneratedQuestion> questions)
    {
        var builder = new List<string>
        {
            $"Quiz title: {quiz.Title}",
            $"Difficulty: {quiz.Difficulty}",
            $"Type: {quiz.Type}"
        };

        if (module != null)
        {
            builder.Add($"Module title: {module.Title}");
            if (!string.IsNullOrWhiteSpace(module.CategoryLabel))
            {
                builder.Add($"Module category: {module.CategoryLabel}");
            }
        }

        var prompts = questions
            .Select(q => q.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Take(3)
            .ToList();

        if (prompts.Count > 0)
        {
            builder.Add("Sample questions:");
            builder.AddRange(prompts.Select(prompt => $"- {prompt}"));
        }

        builder.Add("Suggest a concise academic category for this quiz.");
        return string.Join("\n", builder);
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