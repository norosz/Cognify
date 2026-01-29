using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognify.Tests.Services;

public class AgentRunServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly AgentRunService _service;

    public AgentRunServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new AgentRunService(_context);
    }

    [Fact]
    public async Task CreateAsync_WhenCalled_ShouldPersistPendingRun()
    {
        var userId = Guid.NewGuid();
        var inputHash = AgentRunService.ComputeHash("sample-input");

        var run = await _service.CreateAsync(userId, AgentRunType.Extraction, inputHash, correlationId: "corr-1", promptVersion: "v1");

        run.Should().NotBeNull();
        run.UserId.Should().Be(userId);
        run.Type.Should().Be(AgentRunType.Extraction);
        run.Status.Should().Be(AgentRunStatus.Pending);
        run.InputHash.Should().Be(inputHash);
        run.CorrelationId.Should().Be("corr-1");
        run.PromptVersion.Should().Be("v1");

        var dbRun = await _context.AgentRuns.FindAsync(run.Id);
        dbRun.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkRunningAsync_WhenCalled_ShouldSetRunningAndModel()
    {
        var run = await _service.CreateAsync(Guid.NewGuid(), AgentRunType.QuizGeneration, AgentRunService.ComputeHash("x"));

        await _service.MarkRunningAsync(run.Id, "model-1");

        var dbRun = await _context.AgentRuns.FindAsync(run.Id);
        dbRun!.Status.Should().Be(AgentRunStatus.Running);
        dbRun.StartedAt.Should().NotBeNull();
        dbRun.Model.Should().Be("model-1");
    }

    [Fact]
    public async Task MarkCompletedAsync_WhenCalled_ShouldSetCompletedAndOutput()
    {
        var run = await _service.CreateAsync(Guid.NewGuid(), AgentRunType.QuizGeneration, AgentRunService.ComputeHash("y"));

        await _service.MarkCompletedAsync(run.Id, "{\"ok\":true}", promptTokens: 10, completionTokens: 20, totalTokens: 30);

        var dbRun = await _context.AgentRuns.FindAsync(run.Id);
        dbRun!.Status.Should().Be(AgentRunStatus.Completed);
        dbRun.CompletedAt.Should().NotBeNull();
        dbRun.OutputJson.Should().Be("{\"ok\":true}");
        dbRun.PromptTokens.Should().Be(10);
        dbRun.CompletionTokens.Should().Be(20);
        dbRun.TotalTokens.Should().Be(30);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenCalled_ShouldSetFailedAndError()
    {
        var run = await _service.CreateAsync(Guid.NewGuid(), AgentRunType.Grading, AgentRunService.ComputeHash("z"));

        await _service.MarkFailedAsync(run.Id, "boom");

        var dbRun = await _context.AgentRuns.FindAsync(run.Id);
        dbRun!.Status.Should().Be(AgentRunStatus.Failed);
        dbRun.CompletedAt.Should().NotBeNull();
        dbRun.ErrorMessage.Should().Be("boom");
    }
}