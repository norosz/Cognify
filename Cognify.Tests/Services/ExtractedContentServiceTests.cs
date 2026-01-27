using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class ExtractedContentServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ExtractedContentService _service;

    public ExtractedContentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new ExtractedContentService(_context);
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
        result.Status.Should().Be("Processing");
        result.UserId.Should().Be(userId);
        result.DocumentId.Should().Be(docId);
        result.ModuleId.Should().Be(modId);
        
        var dbRecord = await _context.ExtractedContents.FindAsync(result.Id);
        dbRecord.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTextAndStatus()
    {
        // Arrange
        var content = new ExtractedContent 
        { 
            Id = Guid.NewGuid(), 
            Status = "Processing",
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
        dbRecord!.Status.Should().Be("Ready");
        dbRecord.Text.Should().Be("Extracted Text");
    }

    [Fact]
    public async Task MarkAsErrorAsync_ShouldUpdateStatusAndErrorMessage()
    {
        // Arrange
        var content = new ExtractedContent 
        { 
            Id = Guid.NewGuid(), 
            Status = "Processing",
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
        dbRecord!.Status.Should().Be("Error");
        dbRecord.ErrorMessage.Should().Be("Extraction Failed");
    }
}
