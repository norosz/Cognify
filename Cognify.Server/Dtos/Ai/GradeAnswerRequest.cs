using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Ai;

public class GradeAnswerRequest
{
    [Required]
    public string Question { get; set; } = string.Empty;
    
    [Required]
    public string Answer { get; set; } = string.Empty;
    
    public string Context { get; set; } = string.Empty;
}
