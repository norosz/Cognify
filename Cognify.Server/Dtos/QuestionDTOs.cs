using System.Text.Json.Serialization;

namespace Cognify.Server.DTOs;

public record QuizQuestionDto
{
    public Guid? Id { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string Type { get; init; } = "MultipleChoice"; // MultipleChoice, TrueFalse, OpenEnded
    public List<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}

public record QuizDto
{
    public Guid Id { get; init; }
    public Guid NoteId { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<QuizQuestionDto> Questions { get; init; } = [];
    public string Type { get; init; } = "MultipleChoice";
    public string Difficulty { get; init; } = "Intermediate";
    public string? QuizRubric { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateQuizDto
{
    public Guid NoteId { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<QuizQuestionDto> Questions { get; init; } = [];
    public string? Difficulty { get; init; }
    public string? QuizRubric { get; init; }
}
