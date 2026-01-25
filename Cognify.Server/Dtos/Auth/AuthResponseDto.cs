namespace Cognify.Server.Dtos.Auth;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required string Email { get; set; }
    public Guid UserId { get; set; }
}
