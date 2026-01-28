using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Auth;

public class ChangePasswordDto
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; set; }
}
