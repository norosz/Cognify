using Cognify.Server.Dtos.Auth;

namespace Cognify.Server.Services.Abstractions;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
