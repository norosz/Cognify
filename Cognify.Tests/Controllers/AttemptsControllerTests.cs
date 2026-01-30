using Cognify.Server.Controllers;
using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
        var reviewService = new Mock<IAttemptReviewService>();
        var controller = new AttemptsController(attemptService.Object, reviewService.Object);

        var result = await controller.SubmitAttempt(Guid.NewGuid(), new SubmitAttemptDto
        {
            QuizId = Guid.NewGuid()
        });

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        problem.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task SubmitAttempt_ReturnsOk_OnSuccess()
    {
        var attemptService = new Mock<IAttemptService>();
        var reviewService = new Mock<IAttemptReviewService>();
        var quizId = Guid.NewGuid();
        var dto = new SubmitAttemptDto { QuizId = quizId };
        var expected = new AttemptDto { Id = Guid.NewGuid(), QuizId = quizId, UserId = Guid.NewGuid(), Score = 100 };

        attemptService.Setup(s => s.SubmitAttemptAsync(dto)).ReturnsAsync(expected);
        var controller = new AttemptsController(attemptService.Object, reviewService.Object);

        var result = await controller.SubmitAttempt(quizId, dto);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SubmitAttempt_ReturnsNotFound_WhenMissing()
    {
        var attemptService = new Mock<IAttemptService>();
        var reviewService = new Mock<IAttemptReviewService>();
        var quizId = Guid.NewGuid();
        var dto = new SubmitAttemptDto { QuizId = quizId };

        attemptService.Setup(s => s.SubmitAttemptAsync(dto)).ThrowsAsync(new KeyNotFoundException());
        var controller = new AttemptsController(attemptService.Object, reviewService.Object);

        var result = await controller.SubmitAttempt(quizId, dto);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        problem.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task GetMyAttempts_ReturnsOk()
    {
        var attemptService = new Mock<IAttemptService>();
        var reviewService = new Mock<IAttemptReviewService>();
        var quizId = Guid.NewGuid();
        var attempts = new List<AttemptDto>
        {
            new AttemptDto { Id = Guid.NewGuid(), QuizId = quizId, UserId = Guid.NewGuid(), Score = 80 }
        };

        attemptService.Setup(s => s.GetAttemptsAsync(quizId)).ReturnsAsync(attempts);
        var controller = new AttemptsController(attemptService.Object, reviewService.Object);

        var result = await controller.GetMyAttempts(quizId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(attempts);
    }
}
