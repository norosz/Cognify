using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Dtos.Exam;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class ExamAttemptService(
    ApplicationDbContext context,
    IUserContextService userContext,
    IKnowledgeStateService knowledgeStateService,
    IAiService aiService,
    IAgentRunService agentRunService) : IExamAttemptService
{
    private const double OpenTextCorrectThreshold = 0.7;
    private const int MaxGradingContextChars = 2000;

    public async Task<ExamAttemptDto> SubmitExamAttemptAsync(SubmitExamAttemptDto dto)
    {
        var userId = userContext.GetCurrentUserId();

        var module = await context.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.ModuleId && m.OwnerUserId == userId);

        if (module == null)
        {
            throw new UnauthorizedAccessException("Module not found or not owned by user.");
        }

        var quiz = await context.Quizzes
            .Include(q => q.Questions)
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId);

        if (quiz == null)
        {
            throw new KeyNotFoundException("Exam quiz not found for module.");
        }

        if (quiz.Note == null)
        {
            if (module.CurrentFinalExamQuizId != quiz.Id)
            {
                throw new KeyNotFoundException("Exam quiz not found for module.");
            }
        }
        else if (quiz.Note.Module?.Id != dto.ModuleId || quiz.Note.Module.OwnerUserId != userId)
        {
            throw new KeyNotFoundException("Exam quiz not found for module.");
        }

        double totalScore = 0;
        int totalQuestions = quiz.Questions.Count;
        var interactions = new List<KnowledgeInteractionInput>();

        string? knownMistakePatterns = null;
        if (quiz.NoteId.HasValue)
        {
            knownMistakePatterns = await context.UserKnowledgeStates
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.SourceNoteId == quiz.NoteId)
                .Select(s => s.MistakePatternsJson)
                .FirstOrDefaultAsync();
        }

        foreach (var question in quiz.Questions)
        {
            var qIdStr = question.Id.ToString();
            string? userAnswer = null;

            if (dto.Answers.TryGetValue(qIdStr, out var ans))
            {
                userAnswer = ans;
            }
            else
            {
                var match = dto.Answers.FirstOrDefault(k => string.Equals(k.Key, qIdStr, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key))
                {
                    userAnswer = match.Value;
                }
            }

            var evaluation = await EvaluateQuestionAsync(userId, question, userAnswer, quiz.RubricJson, knownMistakePatterns);
            totalScore += evaluation.Score;

            interactions.Add(new KnowledgeInteractionInput
            {
                QuestionId = question.Id,
                UserAnswer = userAnswer,
                IsCorrect = evaluation.IsCorrect,
                Score = evaluation.Score,
                MaxScore = 1,
                Feedback = evaluation.Feedback,
                DetectedMistakes = evaluation.DetectedMistakes,
                ConfidenceEstimate = evaluation.ConfidenceEstimate
            });
        }

        double score = totalQuestions > 0 ? (totalScore / totalQuestions) * 100 : 0;

        var attempt = new ExamAttempt
        {
            UserId = userId,
            ModuleId = dto.ModuleId,
            QuizId = dto.QuizId,
            AnswersJson = JsonSerializer.Serialize(dto.Answers),
            Score = score,
            TimeSpentSeconds = dto.TimeSpentSeconds,
            Difficulty = dto.Difficulty ?? quiz.Difficulty.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        context.ExamAttempts.Add(attempt);
        await context.SaveChangesAsync();

        await knowledgeStateService.RecordExamAttemptAsync(attempt, quiz, interactions);

        return MapToDto(attempt);
    }

    public async Task<List<ExamAttemptDto>> GetExamAttemptsAsync(Guid moduleId)
    {
        var userId = userContext.GetCurrentUserId();

        var attempts = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ModuleId == moduleId && a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return attempts.Select(MapToDto).ToList();
    }

    public async Task<ExamAttemptDto?> GetExamAttemptByIdAsync(Guid moduleId, Guid examAttemptId)
    {
        var userId = userContext.GetCurrentUserId();

        var attempt = await context.ExamAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == examAttemptId && a.ModuleId == moduleId && a.UserId == userId);

        return attempt == null ? null : MapToDto(attempt);
    }

    private static ExamAttemptDto MapToDto(ExamAttempt attempt)
    {
        return new ExamAttemptDto
        {
            Id = attempt.Id,
            ModuleId = attempt.ModuleId,
            QuizId = attempt.QuizId,
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

    private async Task<QuestionEvaluation> EvaluateQuestionAsync(
        Guid userId,
        QuizQuestion question,
        string? userAnswer,
        string? quizRubric,
        string? knownMistakePatterns)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return new QuestionEvaluation(0, false, null, null, null);
        }

        var correctAnswer = TryDeserializeString(question.CorrectAnswerJson);
        var normalizedUser = Normalize(userAnswer);
        var normalizedCorrect = Normalize(correctAnswer);

        return question.Type switch
        {
            QuestionType.TrueFalse or QuestionType.MultipleChoice =>
                ScoreFromBoolean(string.Equals(normalizedUser, normalizedCorrect, StringComparison.OrdinalIgnoreCase)),

            QuestionType.Ordering =>
                ScoreFromBoolean(SequenceEquals(SplitAnswer(userAnswer), SplitAnswer(correctAnswer))),

            QuestionType.MultipleSelect =>
                ScoreFromBoolean(SetEquals(SplitAnswer(userAnswer), SplitAnswer(correctAnswer))),

            QuestionType.Matching =>
                ScoreFromBoolean(PairsEqual(ParsePairs(userAnswer), ParsePairs(correctAnswer))),

            QuestionType.OpenText =>
                await ScoreOpenTextAsync(userId, question, userAnswer, correctAnswer, quizRubric, knownMistakePatterns),

            _ => ScoreFromBoolean(string.Equals(normalizedUser, normalizedCorrect, StringComparison.OrdinalIgnoreCase))
        };
    }

    private async Task<QuestionEvaluation> ScoreOpenTextAsync(
        Guid userId,
        QuizQuestion question,
        string userAnswer,
        string correctAnswer,
        string? quizRubric,
        string? knownMistakePatterns)
    {
        var contextText = BuildOpenTextContext(question, correctAnswer, quizRubric);
        var inputHash = AgentRunService.ComputeHash($"grading:{AgentContractVersions.V2}:{question.Id}:{userAnswer}:{correctAnswer}");
        var run = await agentRunService.CreateAsync(userId, AgentRunType.Grading, inputHash, correlationId: question.Id.ToString(), promptVersion: "grading-v2");

        try
        {
            var request = new GradingContractRequest(
                AgentContractVersions.V2,
                question.Prompt,
                userAnswer,
                correctAnswer,
                contextText,
                KnownMistakePatterns: knownMistakePatterns);

            var response = await aiService.GradeAnswerAsync(request);
            if (response == null)
            {
                await agentRunService.MarkFailedAsync(run.Id, "AI grading returned null response.");
                return new QuestionEvaluation(0, false, null, BuildOpenTextMistakes(0), null);
            }

            var normalizedScore = response.MaxScore > 0
                ? Math.Clamp(response.Score / response.MaxScore, 0, 1)
                : 0;

            var output = JsonSerializer.Serialize(new
            {
                response.ContractVersion,
                response.Score,
                response.MaxScore,
                normalizedScore,
                response.Feedback,
                response.DetectedMistakes,
                response.ConfidenceEstimate,
                response.RawAnalysis
            });

            await agentRunService.MarkCompletedAsync(run.Id, output);
            return new QuestionEvaluation(
                normalizedScore,
                normalizedScore >= OpenTextCorrectThreshold,
                response.Feedback,
                response.DetectedMistakes ?? BuildOpenTextMistakes(normalizedScore),
                response.ConfidenceEstimate);
        }
        catch (Exception ex)
        {
            await agentRunService.MarkFailedAsync(run.Id, ex.Message);
            return new QuestionEvaluation(0, false, null, null, null);
        }
    }

    private static string BuildOpenTextContext(QuizQuestion question, string correctAnswer, string? quizRubric)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Reference Answer:");
        builder.AppendLine(correctAnswer);

        if (!string.IsNullOrWhiteSpace(quizRubric))
        {
            builder.AppendLine();
            builder.AppendLine("Rubric:");
            builder.AppendLine(quizRubric);
        }

        var context = builder.ToString();
        if (context.Length <= MaxGradingContextChars)
        {
            return context;
        }

        return context[..MaxGradingContextChars];
    }

    private static QuestionEvaluation ScoreFromBoolean(bool isCorrect)
    {
        return new QuestionEvaluation(isCorrect ? 1 : 0, isCorrect, null, null, null);
    }

    private static bool SequenceEquals(IEnumerable<string> a, IEnumerable<string> b)
    {
        return a.SequenceEqual(b, StringComparer.OrdinalIgnoreCase);
    }

    private static bool SetEquals(IEnumerable<string> a, IEnumerable<string> b)
    {
        return new HashSet<string>(a, StringComparer.OrdinalIgnoreCase)
            .SetEquals(new HashSet<string>(b, StringComparer.OrdinalIgnoreCase));
    }

    private static bool PairsEqual(IEnumerable<(string Left, string Right)> a, IEnumerable<(string Left, string Right)> b)
    {
        var leftMap = a.ToDictionary(x => x.Left, x => x.Right, StringComparer.OrdinalIgnoreCase);
        foreach (var (left, right) in b)
        {
            if (!leftMap.TryGetValue(left, out var mapped) || !string.Equals(mapped, right, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return leftMap.Count == b.Count();
    }

    private static List<string> SplitAnswer(string answer)
    {
        return answer.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static List<(string Left, string Right)> ParsePairs(string input)
    {
        var pairs = new List<(string Left, string Right)>();
        var segments = input.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                pairs.Add((parts[0], parts[1]));
            }
        }
        return pairs;
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), "\\s+", " ");
    }

    private static string TryDeserializeString(string json)
    {
        try { return JsonSerializer.Deserialize<string>(json) ?? string.Empty; }
        catch { return json; }
    }

    private static List<string> BuildOpenTextMistakes(double normalizedScore)
    {
        if (normalizedScore >= OpenTextCorrectThreshold)
        {
            return [];
        }

        return ["NeedsMoreDetail"];
    }

    private readonly record struct QuestionEvaluation(
        double Score,
        bool IsCorrect,
        string? Feedback,
        IReadOnlyList<string>? DetectedMistakes,
        double? ConfidenceEstimate);
}
