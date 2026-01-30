using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cognify.Tests.Services;

public class ConceptClusteringServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUserContextService> _userContext;
    private readonly Guid _userId;
    private readonly ConceptClusteringService _service;

    public ConceptClusteringServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _userContext = new Mock<IUserContextService>();
        _userId = Guid.NewGuid();
        _userContext.Setup(x => x.GetCurrentUserId()).Returns(_userId);

        _service = new ConceptClusteringService(_context, _userContext.Object);
    }

    [Fact]
    public async Task RefreshConceptClustersAsync_CreatesClusters_AndUpdatesKnowledgeStates()
    {
        var user = new User { Id = _userId, Email = "user@example.com", PasswordHash = "hash" };
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = _userId, Title = "Module" };
        var note = new Note { Id = Guid.NewGuid(), ModuleId = module.Id, Title = "Note", Content = "Content" };

        _context.Users.Add(user);
        _context.Modules.Add(module);
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        var noteId = note.Id;
        _context.UserKnowledgeStates.AddRange(
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Linear Algebra",
                SourceNoteId = noteId,
                MasteryScore = 0.4,
                ConfidenceScore = 0.4,
                ForgettingRisk = 0.6,
                UpdatedAt = DateTime.UtcNow
            },
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Algebra Basics",
                SourceNoteId = noteId,
                MasteryScore = 0.5,
                ConfidenceScore = 0.5,
                ForgettingRisk = 0.5,
                UpdatedAt = DateTime.UtcNow
            },
            new UserKnowledgeState
            {
                UserId = _userId,
                Topic = "Cell Biology",
                SourceNoteId = noteId,
                MasteryScore = 0.6,
                ConfidenceScore = 0.6,
                ForgettingRisk = 0.3,
                UpdatedAt = DateTime.UtcNow
            });

        await _context.SaveChangesAsync();

        var clusters = await _service.RefreshConceptClustersAsync(module.Id, true);

        clusters.Should().NotBeEmpty();
        clusters.SelectMany(c => c.Topics).Should().Contain(new[] { "Linear Algebra", "Algebra Basics", "Cell Biology" });

        var states = await _context.UserKnowledgeStates.ToListAsync();
        states.Count(s => s.ConceptClusterId != null).Should().Be(3);
    }

    [Fact]
    public async Task GetConceptClustersAsync_ReturnsEmpty_WhenModuleNotOwned()
    {
        var module = new Module { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Title = "Module" };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        var result = await _service.GetConceptClustersAsync(module.Id);

        result.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
