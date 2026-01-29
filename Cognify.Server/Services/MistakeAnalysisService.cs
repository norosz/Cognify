using System.Text.Json;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class MistakeAnalysisService : IMistakeAnalysisService
{
    public Dictionary<string, int> UpdateMistakePatterns(string? existingJson, IReadOnlyCollection<KnowledgeInteractionInput> interactions)
    {
        var patterns = ParseMistakePatterns(existingJson);
        if (interactions.Count == 0)
        {
            return patterns;
        }

        foreach (var interaction in interactions)
        {
            if (interaction.IsCorrect)
            {
                continue;
            }

            patterns["incorrectCount"] = patterns.GetValueOrDefault("incorrectCount") + 1;

            var mistakes = interaction.DetectedMistakes?.ToList() ?? DetectMistakes(interaction);
            foreach (var mistake in mistakes)
            {
                patterns[mistake] = patterns.GetValueOrDefault(mistake) + 1;
            }
        }

        return patterns;
    }

    public string SerializeMistakePatterns(Dictionary<string, int> patterns)
    {
        return JsonSerializer.Serialize(patterns);
    }

    public List<string> DetectMistakes(KnowledgeInteractionInput interaction)
    {
        if (interaction.IsCorrect)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(interaction.UserAnswer))
        {
            return ["Unanswered"];
        }

        return ["IncorrectAnswer"];
    }

    private static Dictionary<string, int> ParseMistakePatterns(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }
}
