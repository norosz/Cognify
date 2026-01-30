using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class ConceptTopic
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConceptClusterId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Topic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConceptCluster? ConceptCluster { get; set; }
}
