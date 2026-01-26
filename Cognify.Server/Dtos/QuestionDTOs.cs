using System.Text.Json.Serialization;

namespace Cognify.Server.DTOs;

public record QuestionDto
{
    public Guid? Id { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string Type { get; init; } = "MultipleChoice"; // MultipleChoice, TrueFalse, OpenEnded
    public List<string> Options { get; init; } = [];
    public string CorrectAnswer { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}

public record QuestionSetDto
{
    public Guid Id { get; init; }
    public Guid NoteId { get; init; }
    public List<QuestionDto> Questions { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public record CreateQuestionSetDto
{
    public Guid NoteId { get; init; }
    public List<QuestionDto> Questions { get; init; } = [];
}
