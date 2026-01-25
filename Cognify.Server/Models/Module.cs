using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Module
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string Title { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? OwnerUser { get; set; }
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
}
