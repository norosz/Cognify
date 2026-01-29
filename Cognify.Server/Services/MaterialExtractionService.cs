using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class MaterialExtractionService(ApplicationDbContext db) : IMaterialExtractionService
{
    public async Task UpsertExtractionAsync(Material material, string extractedText, string? imagesJson)
    {
        var existing = await db.MaterialExtractions.FirstOrDefaultAsync(e => e.MaterialId == material.Id);
        if (existing == null)
        {
            existing = new MaterialExtraction
            {
                MaterialId = material.Id
            };
            db.MaterialExtractions.Add(existing);
        }

        existing.ExtractedText = extractedText;
        existing.ImagesJson = imagesJson;
        existing.CreatedAt = DateTime.UtcNow;

        material.Status = MaterialStatus.Processed;
        material.HasEmbeddedImages = !string.IsNullOrWhiteSpace(imagesJson);
        material.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}