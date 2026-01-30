using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Categories;

public record CategorySuggestionItemDto
{
    public required string Label { get; init; }
    public double Confidence { get; init; }
    public string? Rationale { get; init; }
}

public record CategoryHistoryItemDto
{
    public required string Label { get; init; }
    public double? Confidence { get; init; }
    public string? Rationale { get; init; }
}

public record CategoryHistoryBatchDto
{
    public required Guid BatchId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Source { get; init; }
    public List<CategoryHistoryItemDto> Items { get; init; } = [];
}

public record CategoryHistoryResponseDto
{
    public List<CategoryHistoryBatchDto> Items { get; init; } = [];
    public Guid? NextCursor { get; init; }
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
