using Cognify.Server.Dtos.Auth;

namespace Cognify.Server.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserProfileDto> GetUserProfileAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}
