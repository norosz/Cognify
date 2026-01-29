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

        var incorrectCount = interactions.Count(i => !i.IsCorrect);
        if (incorrectCount > 0)
        {
            patterns["incorrectCount"] = patterns.GetValueOrDefault("incorrectCount") + incorrectCount;
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
