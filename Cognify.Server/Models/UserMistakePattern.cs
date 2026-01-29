using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class UserMistakePattern
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Topic { get; set; }

    public Guid? SourceNoteId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Category { get; set; }

    public int Count { get; set; }

    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
