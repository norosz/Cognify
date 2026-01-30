namespace Cognify.Server.Dtos.Concepts;

public record ConceptClusterDto
{
    public Guid Id { get; init; }
    public Guid ModuleId { get; init; }
    public string Label { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<string> Topics { get; init; } = [];
}

public record RefreshConceptsRequest
{
    public bool ForceRegenerate { get; init; } = true;
}
