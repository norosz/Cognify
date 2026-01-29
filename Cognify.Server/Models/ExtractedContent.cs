using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

/// <summary>
/// Represents extracted text from a document that hasn't been saved as a Note yet.
/// </summary>
public class ExtractedContent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid DocumentId { get; set; }

    public Guid ModuleId { get; set; }

    public Guid? AgentRunId { get; set; }

    public string? Text { get; set; }

    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    public ExtractedContentStatus Status { get; set; } = ExtractedContentStatus.Ready;

    public string? ErrorMessage { get; set; }

    /// <summary>
    /// True once converted to a Note (soft-delete marker).
    /// </summary>
    public bool IsSaved { get; set; } = false;

    // Navigation properties
    public User? User { get; set; }
    public Document? Document { get; set; }
    public Module? Module { get; set; }

    public AgentRun? AgentRun { get; set; }
}
