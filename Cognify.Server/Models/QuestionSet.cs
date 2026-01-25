using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class QuestionSet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NoteId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Note? Note { get; set; }
    public ICollection<Question> Questions { get; set; } = [];
}
