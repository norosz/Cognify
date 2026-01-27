using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Auth;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Cognify.Server.Services;

public class AuthService(
    ApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await context.Users.AnyAsync(u => u.Email == dto.Email);
        if (existingUser)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHasher.HashPassword(dto.Password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            UserId = user.Id
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            UserId = user.Id
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
