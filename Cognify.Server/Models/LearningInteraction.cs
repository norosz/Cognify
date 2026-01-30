using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class LearningInteraction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Topic { get; set; }

    public LearningInteractionType Type { get; set; } = LearningInteractionType.QuizAnswer;

    public Guid? AttemptId { get; set; }

    public Guid? ExamAttemptId { get; set; }

    public Guid? QuestionId { get; set; }

    public string? UserAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Attempt? Attempt { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }
}

public enum LearningInteractionType
{
    QuizAnswer = 0,
    SelfEvaluation = 1,
    NoteReview = 2
}
