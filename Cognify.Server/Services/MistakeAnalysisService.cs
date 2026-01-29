using System.Text.Json;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class MistakeAnalysisService : IMistakeAnalysisService
{
    public IReadOnlyList<string> Analyze(Question? question, KnowledgeInteractionInput interaction)
    {
        if (interaction.IsCorrect)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(interaction.UserAnswer))
        {
            return ["Unanswered"];
        }

        if (question == null)
        {
            return ["IncorrectAnswer"];
        }

        var correct = TryDeserializeString(question.CorrectAnswerJson);

        return question.Type switch
        {
            QuestionType.TrueFalse or QuestionType.MultipleChoice => ["IncorrectAnswer"],
            QuestionType.OpenText => ["OpenTextIncorrect"],
            QuestionType.Ordering => DetectOrderingMistakes(interaction.UserAnswer, correct),
            QuestionType.MultipleSelect => DetectMultipleSelectMistakes(interaction.UserAnswer, correct),
            QuestionType.Matching => DetectMatchingMistakes(interaction.UserAnswer, correct),
            _ => ["IncorrectAnswer"]
        };
    }

    private static List<string> DetectOrderingMistakes(string userAnswer, string correctAnswer)
    {
        var userSequence = SplitAnswer(userAnswer);
        var correctSequence = SplitAnswer(correctAnswer);

        if (userSequence.Count == 0 || correctSequence.Count == 0)
        {
            return ["OrderMismatch"];
        }

        var userSet = new HashSet<string>(userSequence, StringComparer.OrdinalIgnoreCase);
        var correctSet = new HashSet<string>(correctSequence, StringComparer.OrdinalIgnoreCase);

        if (userSet.SetEquals(correctSet))
        {
            return ["OrderMismatch"];
        }

        return ["IncorrectItems"];
    }

    private static List<string> DetectMultipleSelectMistakes(string userAnswer, string correctAnswer)
    {
        var userSet = new HashSet<string>(SplitAnswer(userAnswer), StringComparer.OrdinalIgnoreCase);
        var correctSet = new HashSet<string>(SplitAnswer(correctAnswer), StringComparer.OrdinalIgnoreCase);

        var mistakes = new List<string>();
        if (!correctSet.IsSubsetOf(userSet))
        {
            mistakes.Add("MissingSelection");
        }

        if (!userSet.IsSubsetOf(correctSet))
        {
            mistakes.Add("ExtraSelection");
        }

        return mistakes.Count > 0 ? mistakes : ["IncorrectAnswer"];
    }

    private static List<string> DetectMatchingMistakes(string userAnswer, string correctAnswer)
    {
        var userPairs = ParsePairs(userAnswer);
        var correctPairs = ParsePairs(correctAnswer);

        if (userPairs.Count == 0 || correctPairs.Count == 0)
        {
            return ["PairingMismatch"];
        }

        foreach (var kvp in correctPairs)
        {
            if (!userPairs.TryGetValue(kvp.Key, out var actual) || !string.Equals(actual, kvp.Value, StringComparison.OrdinalIgnoreCase))
            {
                return ["PairingMismatch"];
            }
        }

        return ["PairingMismatch"];
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
}
