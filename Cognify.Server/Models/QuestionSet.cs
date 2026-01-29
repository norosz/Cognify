using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NoteId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    public QuizDifficulty Difficulty { get; set; } = QuizDifficulty.Intermediate;

    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

    public string? RubricJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Note? Note { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; } = [];
}
