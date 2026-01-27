using System.Text.Json.Serialization;
using Cognify.Server.Models;

namespace Cognify.Server.Models.Ai;

public class GeneratedQuestion
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public QuestionType Type { get; set; }
    
    // For MC/TF
    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("correctAnswer")]
    public object? CorrectAnswer { get; set; } 

    // For Matching: List of "Term:Definition"
    [JsonPropertyName("pairs")]
    public List<string>? Pairs { get; set; }
    
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("difficultyLevel")]
    public int DifficultyLevel { get; set; } // 1-3
}

public class AssessmentBundle
{
    [JsonPropertyName("questions")]
    public List<GeneratedQuestion> Questions { get; set; } = [];
}
