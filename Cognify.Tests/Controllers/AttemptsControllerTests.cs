using Cognify.Server.Controllers;
using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class AttemptsControllerTests
{
    [Fact]
    public async Task SubmitAttempt_ReturnsBadRequest_OnIdMismatch()
    {
        var attemptService = new Mock<IAttemptService>();
        var controller = new AttemptsController(attemptService.Object);

        var result = await controller.SubmitAttempt(Guid.NewGuid(), new SubmitAttemptDto
        {
            QuestionSetId = Guid.NewGuid()
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SubmitAttempt_ReturnsOk_OnSuccess()
    {
        var attemptService = new Mock<IAttemptService>();
        var questionSetId = Guid.NewGuid();
        var dto = new SubmitAttemptDto { QuestionSetId = questionSetId };
        var expected = new AttemptDto { Id = Guid.NewGuid(), QuestionSetId = questionSetId, UserId = Guid.NewGuid(), Score = 100 };

        attemptService.Setup(s => s.SubmitAttemptAsync(dto)).ReturnsAsync(expected);
        var controller = new AttemptsController(attemptService.Object);

        var result = await controller.SubmitAttempt(questionSetId, dto);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SubmitAttempt_ReturnsNotFound_WhenMissing()
    {
        var attemptService = new Mock<IAttemptService>();
        var questionSetId = Guid.NewGuid();
        var dto = new SubmitAttemptDto { QuestionSetId = questionSetId };

        attemptService.Setup(s => s.SubmitAttemptAsync(dto)).ThrowsAsync(new KeyNotFoundException());
        var controller = new AttemptsController(attemptService.Object);

        var result = await controller.SubmitAttempt(questionSetId, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMyAttempts_ReturnsOk()
    {
        var attemptService = new Mock<IAttemptService>();
        var questionSetId = Guid.NewGuid();
        var attempts = new List<AttemptDto>
        {
            new AttemptDto { Id = Guid.NewGuid(), QuestionSetId = questionSetId, UserId = Guid.NewGuid(), Score = 80 }
        };

        attemptService.Setup(s => s.GetAttemptsAsync(questionSetId)).ReturnsAsync(attempts);
        var controller = new AttemptsController(attemptService.Object);

        var result = await controller.GetMyAttempts(questionSetId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(attempts);
    }
}
