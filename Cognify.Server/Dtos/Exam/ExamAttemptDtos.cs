using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Exam;

public record ExamAttemptDto
{
    public Guid Id { get; init; }
    public Guid ModuleId { get; init; }
    public Guid QuizId { get; init; }
    public Guid UserId { get; init; }
    public double Score { get; init; }
    public Dictionary<string, string> Answers { get; init; } = [];
    public int? TimeSpentSeconds { get; init; }
    public string? Difficulty { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SubmitExamAttemptDto
{
    [Required]
    public Guid ModuleId { get; init; }

    [Required]
    public Guid QuizId { get; init; }

    [Required]
    public Dictionary<string, string> Answers { get; init; } = [];

    public int? TimeSpentSeconds { get; init; }
    public string? Difficulty { get; init; }
}
