using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IBlobStorageService> _blobStorage;
    private readonly Mock<IUserContextService> _userContext;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _blobStorage = new Mock<IBlobStorageService>();
        _userContext = new Mock<IUserContextService>();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = ownerId, Title = "M" };
        var doc = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "file.pdf", BlobPath = "m/doc_file.pdf", Status = DocumentStatus.Uploaded, CreatedAt = DateTime.UtcNow };

        _context.Modules.Add(module);
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        _userContext.Setup(x => x.GetCurrentUserId()).Returns(otherId);
        var service = new DocumentService(_context, _blobStorage.Object, _userContext.Object);

        var result = await service.GetByIdAsync(doc.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenOwner()
    {
        var ownerId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = ownerId, Title = "M" };
        var doc = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "file.pdf", BlobPath = "m/doc_file.pdf", Status = DocumentStatus.Uploaded, CreatedAt = DateTime.UtcNow };

        _context.Modules.Add(module);
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        _userContext.Setup(x => x.GetCurrentUserId()).Returns(ownerId);
        _blobStorage.Setup(x => x.GenerateDownloadSasToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string?>())).Returns("url");

        var service = new DocumentService(_context, _blobStorage.Object, _userContext.Object);
        var result = await service.GetByIdAsync(doc.Id);

        result.Should().NotBeNull();
        result!.DownloadUrl.Should().Be("url");
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesDocument_WhenOwner()
    {
        var ownerId = Guid.NewGuid();
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = ownerId, Title = "M" };
        var doc = new Document { Id = Guid.NewGuid(), ModuleId = module.Id, FileName = "file.pdf", BlobPath = "blob", Status = DocumentStatus.Uploaded, CreatedAt = DateTime.UtcNow };

        _context.Modules.Add(module);
        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        _userContext.Setup(x => x.GetCurrentUserId()).Returns(ownerId);
        var service = new DocumentService(_context, _blobStorage.Object, _userContext.Object);

        await service.DeleteDocumentAsync(doc.Id);

        var dbRecord = await _context.Documents.FindAsync(doc.Id);
        dbRecord.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
