using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ModuleId { get; set; }

    public Guid? SourceDocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string FileName { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    [Required]
    public required string BlobPath { get; set; }

    public MaterialStatus Status { get; set; } = MaterialStatus.Uploaded;

    public bool HasEmbeddedImages { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Module? Module { get; set; }
    public Document? SourceDocument { get; set; }
    public ICollection<MaterialExtraction> Extractions { get; set; } = [];
}

public enum MaterialStatus
{
    Uploaded,
    Processed,
    Failed
}