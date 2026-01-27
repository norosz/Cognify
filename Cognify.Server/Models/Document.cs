using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    [Required]
    public required string FileName { get; set; }

    [Required]
    public required string BlobPath { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Processing;

    public string? ExtractedText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Module? Module { get; set; }
}

public enum DocumentStatus
{
    Processing,
    Ready,
    Error
}
