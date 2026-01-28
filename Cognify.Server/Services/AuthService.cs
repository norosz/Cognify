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

        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            var existingUsername = await context.Users.AnyAsync(u => u.Username == dto.Username);
            if (existingUsername)
            {
                throw new InvalidOperationException("Username is already taken.");
            }
        }

        var user = new User
        {
            Email = dto.Email,
            Username = dto.Username,
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
            new Claim("username", user.Username ?? ""),
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

    public async Task<UserProfileDto> GetUserProfileAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username
        };
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid current password.");
        }

        user.PasswordHash = passwordHasher.HashPassword(dto.NewPassword);
        await context.SaveChangesAsync();
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (user.Email != dto.Email)
        {
            var emailExists = await context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId);
            if (emailExists)
            {
                throw new InvalidOperationException("Email is already in use.");
            }
            user.Email = dto.Email;
        }

        user.Username = dto.Username;

        await context.SaveChangesAsync();

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username
        };
    }
}
