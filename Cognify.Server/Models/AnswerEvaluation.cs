using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class AnswerEvaluation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LearningInteractionId { get; set; }

    public double Score { get; set; }

    public double MaxScore { get; set; }

    [MaxLength(2000)]
    public string? Feedback { get; set; }

    public string? DetectedMistakesJson { get; set; }

    public double? ConfidenceEstimate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public LearningInteraction? LearningInteraction { get; set; }
}
