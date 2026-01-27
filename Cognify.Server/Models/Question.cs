using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionSetId { get; set; }

    public QuestionType Type { get; set; }

    [Required]
    public required string Prompt { get; set; }

    // JSON stored as string
    public required string OptionsJson { get; set; }

    // JSON stored as string
    public required string CorrectAnswerJson { get; set; }

    public string? Explanation { get; set; }

    // Navigation properties
    public QuestionSet? QuestionSet { get; set; }
}

public enum QuestionType
{
    Mixed = -1,
    MultipleChoice = 0,
    TrueFalse = 1,
    OpenText = 2,
    Matching = 3,
    Ordering = 4
}
