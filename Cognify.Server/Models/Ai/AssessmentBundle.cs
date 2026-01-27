using System.Text.Json.Serialization;
using Cognify.Server.Models;

namespace Cognify.Server.Models.Ai;

public class GeneratedQuestion
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    
    // For MC/TF
    public List<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; } 

    // For Matching: List of "Term:Definition"
    public List<string>? Pairs { get; set; }
    
    public string Explanation { get; set; } = string.Empty;
    public int DifficultyLevel { get; set; } // 1-3
}

public class AssessmentBundle
{
    public List<GeneratedQuestion> Questions { get; set; } = [];
}
