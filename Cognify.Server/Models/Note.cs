using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    public string? Content { get; set; }

    public string? UserContent { get; set; }

    public string? AiContent { get; set; }

    public Guid? SourceMaterialId { get; set; }

    public string? EmbeddedImagesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Module? Module { get; set; }
    public ICollection<Quiz> Quizzes { get; set; } = [];
    public Material? SourceMaterial { get; set; }
}
