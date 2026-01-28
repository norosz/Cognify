using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public required string Email { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Module> Modules { get; set; } = [];
}
