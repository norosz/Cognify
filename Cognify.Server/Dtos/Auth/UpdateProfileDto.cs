using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Auth;

public class UpdateProfileDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    public string? Username { get; set; }
}
