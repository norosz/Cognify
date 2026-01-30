using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class ExamAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModuleId { get; set; }

    public Guid QuizId { get; set; }

    public Guid UserId { get; set; }

    // JSON stored as string
    public required string AnswersJson { get; set; }

    public double Score { get; set; }

    public int? TimeSpentSeconds { get; set; }

    [MaxLength(50)]
    public string? Difficulty { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Module? Module { get; set; }
    public Quiz? Quiz { get; set; }
    public User? User { get; set; }
}
