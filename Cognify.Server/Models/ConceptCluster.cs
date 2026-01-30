using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class ConceptCluster
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Label { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Module? Module { get; set; }
    public ICollection<ConceptTopic> Topics { get; set; } = [];
}
