using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class Attempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionSetId { get; set; }

    public Guid UserId { get; set; }

    // JSON stored as string
    public required string AnswersJson { get; set; }

    public double Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public QuestionSet? QuestionSet { get; set; }
    public User? User { get; set; }
}
