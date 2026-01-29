using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognify.Tests.Services;

public class MaterialServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public MaterialServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task EnsureForDocumentAsync_CreatesMaterial_WhenMissing()
    {
        var userId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = userId, Title = "M" };
        var doc = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "file.pdf", BlobPath = "blob", Status = DocumentStatus.Uploaded, CreatedAt = DateTime.UtcNow };
        _context.Modules.Add(module);
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        var service = new MaterialService(_context);
        var material = await service.EnsureForDocumentAsync(doc.Id, userId);

        material.SourceDocumentId.Should().Be(doc.Id);
        material.ContentType.Should().Be("application/pdf");
        material.Status.Should().Be(MaterialStatus.Uploaded);
    }

    [Fact]
    public async Task EnsureForDocumentAsync_ReturnsExisting_WhenFound()
    {
        var userId = Guid.NewGuid();
        var material = new Material
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ModuleId = Guid.NewGuid(),
            SourceDocumentId = Guid.NewGuid(),
            FileName = "file.txt",
            BlobPath = "blob",
            Status = MaterialStatus.Failed
        };
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();

        var service = new MaterialService(_context);
        var result = await service.EnsureForDocumentAsync(material.SourceDocumentId!.Value, userId);

        result.Id.Should().Be(material.Id);
        result.Status.Should().Be(MaterialStatus.Uploaded);
    }

    [Fact]
    public async Task EnsureForDocumentAsync_ShouldThrow_WhenDocumentNotOwned()
    {
        var userId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "M" };
        var doc = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "file.pdf", BlobPath = "blob" };
        _context.Modules.Add(module);
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        var service = new MaterialService(_context);
        Func<Task> act = async () => await service.EnsureForDocumentAsync(doc.Id, userId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetByDocumentIdAsync_ShouldReturnMaterial_WhenFound()
    {
        var userId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var material = new Material
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ModuleId = Guid.NewGuid(),
            SourceDocumentId = documentId,
            FileName = "file.txt",
            BlobPath = "blob",
            Status = MaterialStatus.Uploaded
        };

        _context.Materials.Add(material);
        await _context.SaveChangesAsync();

        var service = new MaterialService(_context);
        var result = await service.GetByDocumentIdAsync(documentId, userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(material.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
