using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognify.Tests.Services;

public class MaterialExtractionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public MaterialExtractionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpsertExtractionAsync_CreatesExtraction_WhenMissing()
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            FileName = "doc.txt",
            BlobPath = "blob",
            Status = MaterialStatus.Uploaded
        };
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();

        var service = new MaterialExtractionService(_context);
        await service.UpsertExtractionAsync(material, "text", "[]", "[]", 0.9);

        var extraction = await _context.MaterialExtractions.FirstOrDefaultAsync(e => e.MaterialId == material.Id);
        extraction.Should().NotBeNull();
        extraction!.ExtractedText.Should().Be("text");
        extraction.BlocksJson.Should().Be("[]");
        extraction.OverallConfidence.Should().Be(0.9);
        material.Status.Should().Be(MaterialStatus.Processed);
        material.HasEmbeddedImages.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertExtractionAsync_UpdatesExisting()
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            FileName = "doc.txt",
            BlobPath = "blob",
            Status = MaterialStatus.Uploaded
        };
        var extraction = new MaterialExtraction { MaterialId = material.Id, ExtractedText = "old" };

        _context.Materials.Add(material);
        _context.MaterialExtractions.Add(extraction);
        await _context.SaveChangesAsync();

        var service = new MaterialExtractionService(_context);
        await service.UpsertExtractionAsync(material, "new", null, null, null);

        var updated = await _context.MaterialExtractions.FirstAsync(e => e.MaterialId == material.Id);
        updated.ExtractedText.Should().Be("new");
        updated.BlocksJson.Should().BeNull();
        updated.OverallConfidence.Should().BeNull();
        material.HasEmbeddedImages.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
