using Cognify.Server.Dtos.Auth;
using Cognify.Server.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await authService.RegisterAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Registration failed", Detail = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ProblemDetails { Title = "Login failed", Detail = ex.Message });
        }
    }
}
