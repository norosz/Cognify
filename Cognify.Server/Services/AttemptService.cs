using Cognify.Server.Data;
using Cognify.Server.DTOs;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Cognify.Server.Services;

public class AttemptService(
    ApplicationDbContext context,
    IUserContextService userContext,
    IKnowledgeStateService knowledgeStateService,
    IAiService aiService,
    IAgentRunService agentRunService) : IAttemptService
{
    private const double OpenTextCorrectThreshold = 0.7;
    private const int MaxGradingContextChars = 2000;

    public async Task<AttemptDto> SubmitAttemptAsync(SubmitAttemptDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        
        var questionSet = await context.QuestionSets
            .Include(qs => qs.Questions)
            .Include(qs => qs.Note)
            .ThenInclude(n => n.Module)
            .FirstOrDefaultAsync(qs => qs.Id == dto.QuestionSetId);

        if (questionSet == null)
             throw new KeyNotFoundException("Question set not found.");

        double totalScore = 0;
        int totalQuestions = questionSet.Questions.Count;

        var interactions = new List<KnowledgeInteractionInput>();

        foreach (var question in questionSet.Questions)
        {
            var qIdStr = question.Id.ToString();
            // Case insensitive key lookup could be nice, but GUIDs are usually standard.
            // We'll iterate keys to find match if needed, but dictionary lookup is better.
            // Assuming frontend sends exact GUID string.
            
            // Try explicit match first
            string? userAnswer = null;
            if (dto.Answers.TryGetValue(qIdStr, out var ans))
            {
                userAnswer = ans;
            }
            else
            {
                // Fallback: Check case-insensitive keys (GUIDs might differ in casing)
                var match = dto.Answers.FirstOrDefault(k => string.Equals(k.Key, qIdStr, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key))
                {
                    userAnswer = match.Value;
                }
            }

            var evaluation = await EvaluateQuestionAsync(userId, question, userAnswer);
            totalScore += evaluation.Score;

            interactions.Add(new KnowledgeInteractionInput
            {
                QuestionId = question.Id,
                UserAnswer = userAnswer,
                IsCorrect = evaluation.IsCorrect
            });
        }

        double score = totalQuestions > 0 ? (totalScore / totalQuestions) * 100 : 0;

        var attempt = new Attempt
        {
            UserId = userId,
            QuestionSetId = dto.QuestionSetId,
            AnswersJson = JsonSerializer.Serialize(dto.Answers),
            Score = score,
            CreatedAt = DateTime.UtcNow
        };

        context.Attempts.Add(attempt);
        await context.SaveChangesAsync();

        await knowledgeStateService.ApplyAttemptResultAsync(attempt, questionSet, interactions);

        return MapToDto(attempt);
    }

    public async Task<List<AttemptDto>> GetAttemptsAsync(Guid questionSetId)
    {
        var userId = userContext.GetCurrentUserId();
        
        var attempts = await context.Attempts
            .AsNoTracking()
            .Where(a => a.QuestionSetId == questionSetId && a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return attempts.Select(MapToDto).ToList();
    }
    
    public async Task<AttemptDto?> GetAttemptByIdAsync(Guid id)
    {
         var userId = userContext.GetCurrentUserId();
         var attempt = await context.Attempts.AsNoTracking()
             .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
             
         return attempt == null ? null : MapToDto(attempt);
    }

    private static AttemptDto MapToDto(Attempt attempt)
    {
        return new AttemptDto
        {
            Id = attempt.Id,
            QuestionSetId = attempt.QuestionSetId,
            UserId = attempt.UserId,
            Score = attempt.Score,
            Answers = TryDeserializeAttempts(attempt.AnswersJson),
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

    private async Task<QuestionEvaluation> EvaluateQuestionAsync(Guid userId, Question question, string? userAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return new QuestionEvaluation(0, false);
        }

        var correctAnswer = TryDeserializeString(question.CorrectAnswerJson);
        var normalizedUser = Normalize(userAnswer);
        var normalizedCorrect = Normalize(correctAnswer);

        switch (question.Type)
        {
            case QuestionType.TrueFalse:
            case QuestionType.MultipleChoice:
                return ScoreFromBoolean(string.Equals(normalizedUser, normalizedCorrect, StringComparison.OrdinalIgnoreCase));

            case QuestionType.Ordering:
                return ScoreFromBoolean(SequenceEquals(SplitAnswer(userAnswer), SplitAnswer(correctAnswer)));

            case QuestionType.MultipleSelect:
                return ScoreFromBoolean(SetEquals(SplitAnswer(userAnswer), SplitAnswer(correctAnswer)));

            case QuestionType.Matching:
                return ScoreFromBoolean(PairsEqual(ParsePairs(userAnswer), ParsePairs(correctAnswer)));

            case QuestionType.OpenText:
                return await ScoreOpenTextAsync(userId, question, userAnswer, correctAnswer);

            default:
                return ScoreFromBoolean(string.Equals(normalizedUser, normalizedCorrect, StringComparison.OrdinalIgnoreCase));
        }
    }

    private async Task<QuestionEvaluation> ScoreOpenTextAsync(Guid userId, Question question, string userAnswer, string correctAnswer)
    {
        var context = BuildOpenTextContext(question, correctAnswer);
        var inputHash = AgentRunService.ComputeHash($"grading:{question.Id}:{userAnswer}:{correctAnswer}");
        var run = await agentRunService.CreateAsync(userId, AgentRunType.Grading, inputHash, correlationId: question.Id.ToString(), promptVersion: "grading-v1");

        try
        {
            var analysis = await aiService.GradeAnswerAsync(question.Prompt, userAnswer, context);
            var parsedScore = TryParseScore(analysis);
            var normalizedScore = parsedScore.HasValue
                ? Math.Clamp(parsedScore.Value / 100.0, 0, 1)
                : 0;

            var output = JsonSerializer.Serialize(new
            {
                score = parsedScore,
                normalizedScore,
                analysis
            });

            await agentRunService.MarkCompletedAsync(run.Id, output);
            return new QuestionEvaluation(normalizedScore, normalizedScore >= OpenTextCorrectThreshold);
        }
        catch (Exception ex)
        {
            await agentRunService.MarkFailedAsync(run.Id, ex.Message);
            return new QuestionEvaluation(0, false);
        }
    }

    private static string BuildOpenTextContext(Question question, string correctAnswer)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Reference Answer:");
        builder.AppendLine(correctAnswer);

        if (!string.IsNullOrWhiteSpace(question.Explanation))
        {
            builder.AppendLine();
            builder.AppendLine("Explanation:");
            builder.AppendLine(question.Explanation);
        }

        var context = builder.ToString();
        if (context.Length > MaxGradingContextChars)
        {
            return context[..MaxGradingContextChars];
        }

        return context;
    }

    private static string Normalize(string value) => value.Trim();

    private static QuestionEvaluation ScoreFromBoolean(bool isCorrect)
    {
        return new QuestionEvaluation(isCorrect ? 1 : 0, isCorrect);
    }

    private static bool SequenceEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private static bool SetEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var setLeft = new HashSet<string>(left, StringComparer.OrdinalIgnoreCase);
        var setRight = new HashSet<string>(right, StringComparer.OrdinalIgnoreCase);
        return setLeft.SetEquals(setRight);
    }

    private static IReadOnlyList<string> SplitAnswer(string value)
    {
        return value
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static Dictionary<string, string> ParsePairs(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in SplitAnswer(value))
        {
            var parts = pair.Split(':');
            if (parts.Length < 2) continue;
            var term = parts[0].Trim();
            var definition = string.Join(':', parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(definition)) continue;
            result[term] = definition;
        }
        return result;
    }

    private static bool PairsEqual(Dictionary<string, string> left, Dictionary<string, string> right)
    {
        if (left.Count != right.Count) return false;
        foreach (var kvp in left)
        {
            if (!right.TryGetValue(kvp.Key, out var other)) return false;
            if (!string.Equals(kvp.Value, other, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    private static string TryDeserializeString(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<string>(json) ?? string.Empty;
        }
        catch
        {
            return json;
        }
    }

    private static int? TryParseScore(string analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis)) return null;

        var match = Regex.Match(analysis, @"Score\s*[:\-]\s*(\d{1,3})", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Clamp(score, 0, 100);
        }

        return null;
    }

    private readonly record struct QuestionEvaluation(double Score, bool IsCorrect);
}
