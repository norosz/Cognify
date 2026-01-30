using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Models;

public class CategorySuggestionItem
{
    public Guid Id { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    public CategorySuggestionBatch? Batch { get; set; }

    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    public double? Confidence { get; set; }

    [MaxLength(2000)]
    public string? Rationale { get; set; }
}
