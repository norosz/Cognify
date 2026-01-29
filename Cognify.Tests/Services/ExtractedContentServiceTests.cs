using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class ExtractedContentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExtractedContentService _service;
    private readonly Mock<IMaterialService> _materialServiceMock;

    public ExtractedContentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var agentRunService = new AgentRunService(_context);
        _materialServiceMock = new Mock<IMaterialService>();
        _service = new ExtractedContentService(_context, agentRunService, _materialServiceMock.Object);
    }

    [Fact]
    public async Task CreatePendingAsync_ShouldCreateRecord_WithProcessingStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var modId = Guid.NewGuid();

        // Act
        var result = await _service.CreatePendingAsync(userId, docId, modId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ExtractedContentStatus.Processing);
        result.UserId.Should().Be(userId);
        result.DocumentId.Should().Be(docId);
        result.ModuleId.Should().Be(modId);
        
        var dbRecord = await _context.ExtractedContents.FindAsync(result.Id);
        dbRecord.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePendingAsync_ShouldReuseExistingPendingContent_ForSameDocument()
    {
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();
        var modId = Guid.NewGuid();

        var existing = new ExtractedContent
        {
            UserId = userId,
            DocumentId = docId,
            ModuleId = modId,
            Status = ExtractedContentStatus.Processing,
            IsSaved = false
        };

        _context.ExtractedContents.Add(existing);
        await _context.SaveChangesAsync();

        var result = await _service.CreatePendingAsync(userId, docId, modId);

        result.Id.Should().Be(existing.Id);
        (await _context.ExtractedContents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTextAndStatus()
    {
        // Arrange
        var content = new ExtractedContent 
        { 
            Id = Guid.NewGuid(), 
            Status = ExtractedContentStatus.Processing,
            UserId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            ExtractedAt = DateTime.UtcNow
        };
        _context.ExtractedContents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateAsync(content.Id, "Extracted Text");

        // Assert
        var dbRecord = await _context.ExtractedContents.FindAsync(content.Id);
        dbRecord!.Status.Should().Be(ExtractedContentStatus.Ready);
        dbRecord.Text.Should().Be("Extracted Text");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeDocumentAndModule()
    {
        var userId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _context.Modules.Add(new Module
        {
            Id = moduleId,
            OwnerUserId = userId,
            Title = "Module"
        });

        _context.Documents.Add(new Document
        {
            Id = documentId,
            ModuleId = moduleId,
            FileName = "doc.pdf",
            BlobPath = "blob"
        });

        var content = new ExtractedContent
        {
            UserId = userId,
            DocumentId = documentId,
            ModuleId = moduleId,
            Status = ExtractedContentStatus.Ready,
            Text = "Text"
        };
        _context.ExtractedContents.Add(content);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(content.Id);

        result.Should().NotBeNull();
        result!.Document.Should().NotBeNull();
        result.Module.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsErrorAsync_ShouldUpdateStatusAndErrorMessage()
    {
        // Arrange
        var content = new ExtractedContent 
        { 
            Id = Guid.NewGuid(), 
            Status = ExtractedContentStatus.Processing,
            UserId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            ExtractedAt = DateTime.UtcNow
        };
        _context.ExtractedContents.Add(content);
        await _context.SaveChangesAsync();

        // Act
        await _service.MarkAsErrorAsync(content.Id, "Extraction Failed");

        // Assert
        var dbRecord = await _context.ExtractedContents.FindAsync(content.Id);
        dbRecord!.Status.Should().Be(ExtractedContentStatus.Error);
        dbRecord.ErrorMessage.Should().Be("Extraction Failed");
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnUnsavedItems()
    {
        var userId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var documentIdSaved = Guid.NewGuid();

        _context.Modules.Add(new Module
        {
            Id = moduleId,
            OwnerUserId = userId,
            Title = "Module"
        });

        _context.Documents.AddRange(
            new Document { Id = documentId, ModuleId = moduleId, FileName = "doc-1.pdf", BlobPath = "blob-1" },
            new Document { Id = documentIdSaved, ModuleId = moduleId, FileName = "doc-2.pdf", BlobPath = "blob-2" }
        );

        _context.ExtractedContents.AddRange(
            new ExtractedContent { UserId = userId, DocumentId = documentId, ModuleId = moduleId, IsSaved = false },
            new ExtractedContent { UserId = userId, DocumentId = documentIdSaved, ModuleId = moduleId, IsSaved = true }
        );
        await _context.SaveChangesAsync();

        var results = await _service.GetByUserAsync(userId);

        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveAsNoteAsync_ShouldCreateNoteAndMarkSaved()
    {
        var userId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var run = new AgentRun { Id = Guid.NewGuid(), UserId = userId, OutputJson = "{\"images\":[{\"id\":\"img\"}]}" };

        _context.AgentRuns.Add(run);
        var content = new ExtractedContent
        {
            UserId = userId,
            DocumentId = documentId,
            ModuleId = moduleId,
            AgentRunId = run.Id,
            Status = ExtractedContentStatus.Ready,
            Text = "Text",
            IsSaved = false
        };
        _context.ExtractedContents.Add(content);
        await _context.SaveChangesAsync();

        _materialServiceMock.Setup(s => s.GetByDocumentIdAsync(documentId, userId))
            .ReturnsAsync(new Material { Id = Guid.NewGuid(), UserId = userId, ModuleId = moduleId, FileName = "doc.pdf", BlobPath = "blob" });

        var note = await _service.SaveAsNoteAsync(content.Id, userId, "Title");

        note.Title.Should().Be("Title");
        note.Content.Should().Be("Text");
        note.EmbeddedImagesJson.Should().Be("[{\"id\":\"img\"}]");

        var updated = await _context.ExtractedContents.FindAsync(content.Id);
        updated!.IsSaved.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveContent()
    {
        var content = new ExtractedContent
        {
            UserId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            IsSaved = false
        };
        _context.ExtractedContents.Add(content);
        await _context.SaveChangesAsync();

        await _service.DeleteAsync(content.Id);

        var dbRecord = await _context.ExtractedContents.FindAsync(content.Id);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task GetCountByUserAsync_ShouldReturnCount()
    {
        var userId = Guid.NewGuid();
        _context.ExtractedContents.AddRange(
            new ExtractedContent { UserId = userId, DocumentId = Guid.NewGuid(), ModuleId = Guid.NewGuid(), IsSaved = false },
            new ExtractedContent { UserId = userId, DocumentId = Guid.NewGuid(), ModuleId = Guid.NewGuid(), IsSaved = true }
        );
        await _context.SaveChangesAsync();

        var count = await _service.GetCountByUserAsync(userId);

        count.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
