using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class AgentRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public AgentRunType Type { get; set; } = AgentRunType.Extraction;

    public AgentRunStatus Status { get; set; } = AgentRunStatus.Pending;

    [MaxLength(128)]
    public string? InputHash { get; set; }

    [MaxLength(50)]
    public string? PromptVersion { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    public string? OutputJson { get; set; }

    public string? ErrorMessage { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public User? User { get; set; }
}