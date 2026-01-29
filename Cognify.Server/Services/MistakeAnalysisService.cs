using System.Text.Json;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class MistakeAnalysisService : IMistakeAnalysisService
{
    private static readonly Dictionary<string, string[]> FeedbackKeywordMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ConceptGap"] = new[] { "concept", "understanding", "misconception" },
        ["DefinitionGap"] = new[] { "definition", "define", "terminology" },
        ["CalculationError"] = new[] { "calculation", "compute", "math", "arithmetic" },
        ["UnitsError"] = new[] { "unit", "units" },
        ["FormulaMisuse"] = new[] { "formula", "equation" },
        ["SignError"] = new[] { "sign", "negative", "positive" },
        ["LogicalError"] = new[] { "logic", "logical" },
        ["ReasoningGap"] = new[] { "reasoning", "explain", "justification" },
        ["VisualInterpretationError"] = new[] { "diagram", "figure", "graph" }
    };

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

        var mistakes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var normalizedScore = interaction.MaxScore > 0
            ? interaction.Score / interaction.MaxScore
            : 0;

        if (normalizedScore > 0)
        {
            mistakes.Add("PartiallyCorrect");
        }
        else
        {
            mistakes.Add("IncorrectAnswer");
        }

        if (interaction.ConfidenceEstimate is < 0.5)
        {
            mistakes.Add("LowConfidence");
        }

        if (interaction.UserAnswer.Trim().Length < 8)
        {
            mistakes.Add("TooShortAnswer");
        }

        if (!string.IsNullOrWhiteSpace(interaction.Feedback))
        {
            foreach (var (category, keywords) in FeedbackKeywordMap)
            {
                if (keywords.Any(keyword => interaction.Feedback.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    mistakes.Add(category);
                }
            }
        }

        return mistakes.ToList();
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
