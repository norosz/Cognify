using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IMaterialExtractionService
{
    Task UpsertExtractionAsync(Material material, string extractedText, string? imagesJson, string? blocksJson, double? confidence);
}