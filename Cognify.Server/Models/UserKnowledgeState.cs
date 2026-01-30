using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class UserKnowledgeState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Topic { get; set; }

    public Guid? SourceNoteId { get; set; }

    public double MasteryScore { get; set; }

    public double ConfidenceScore { get; set; }

    public double ForgettingRisk { get; set; }

    public DateTime? NextReviewAt { get; set; }

    public DateTime? LastReviewedAt { get; set; }

    public string? MistakePatternsJson { get; set; }

    public Guid? ConceptClusterId { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public ConceptCluster? ConceptCluster { get; set; }
}
