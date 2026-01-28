namespace Cognify.Server.Dtos.Auth;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public string? Username { get; set; } // Optional/nullable if not enforced yet
}
