using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class CategorySuggestionBatch
{
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public Guid? ModuleId { get; set; }

    public Guid? QuizId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<CategorySuggestionItem> Items { get; set; } = new();
}
