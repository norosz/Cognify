namespace Cognify.Server.Models;

public class MaterialExtraction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MaterialId { get; set; }

    public string? ExtractedText { get; set; }

    public string? BlocksJson { get; set; }

    public double? OverallConfidence { get; set; }

    public string? ImagesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Material? Material { get; set; }
}