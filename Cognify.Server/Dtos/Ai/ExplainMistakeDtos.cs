using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Ai;

public record ExplainMistakeRequest
{
    [Required]
    [MaxLength(4000)]
    public required string QuestionPrompt { get; init; }

    [Required]
    [MaxLength(4000)]
    public required string UserAnswer { get; init; }

    [Required]
    [MaxLength(4000)]
    public required string CorrectAnswer { get; init; }

    public List<string> DetectedMistakes { get; init; } = [];

    [MaxLength(200)]
    public string? ConceptLabel { get; init; }

    [MaxLength(4000)]
    public string? NoteContext { get; init; }

    [MaxLength(4000)]
    public string? ModuleContext { get; init; }
}

public record ExplainMistakeResponse
{
    public required string ExplanationMarkdown { get; init; }
    public List<string> KeyTakeaways { get; init; } = [];
    public List<string> NextSteps { get; init; } = [];
}
