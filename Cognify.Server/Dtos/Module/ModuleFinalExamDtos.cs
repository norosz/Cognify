using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Modules;

public record FinalExamDto
{
    public Guid ModuleId { get; init; }
    public Guid? CurrentQuizId { get; init; }
    public string? CurrentQuizTitle { get; init; }
    public DateTime? CurrentQuizCreatedAt { get; init; }
}

public record RegenerateFinalExamRequest
{
    [Required]
    [Range(1, 100)]
    public int QuestionCount { get; init; }

    [Required]
    [MaxLength(50)]
    public required string Difficulty { get; init; }

    [Required]
    [MaxLength(50)]
    public required string QuestionType { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }
}

public record FinalExamSaveResultDto
{
    public Guid QuizId { get; init; }
}
