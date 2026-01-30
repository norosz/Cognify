using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Notes;

public class UpdateNoteDto
{
    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    public string? Content { get; set; }

    public string? UserContent { get; set; }

    public string? AiContent { get; set; }
}
