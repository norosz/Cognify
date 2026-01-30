using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Categories;

public record CategorySuggestionItemDto
{
    public required string Label { get; init; }
    public double Confidence { get; init; }
    public string? Rationale { get; init; }
}

public record CategorySuggestionResponse
{
    public List<CategorySuggestionItemDto> Suggestions { get; init; } = [];
}

public record CategorySuggestionRequest
{
    [Range(1, 10)]
    public int MaxSuggestions { get; init; } = 5;
}

public record SetCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public required string CategoryLabel { get; init; }
}
