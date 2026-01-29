using Cognify.Server.Controllers;
using Cognify.Server.Dtos.Adaptive;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class AdaptiveQuizzesControllerTests
{
    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnBadRequest_WhenQuestionCountInvalid()
    {
        var service = new Mock<IAdaptiveQuizService>();
        var controller = new AdaptiveQuizzesController(service.Object);

        var request = new AdaptiveQuizRequest
        {
            Mode = AdaptiveQuizMode.Review,
            QuestionCount = 0,
            MaxTopics = 5
        };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnBadRequest_WhenMaxTopicsInvalid()
    {
        var service = new Mock<IAdaptiveQuizService>();
        var controller = new AdaptiveQuizzesController(service.Object);

        var request = new AdaptiveQuizRequest
        {
            Mode = AdaptiveQuizMode.Review,
            QuestionCount = 5,
            MaxTopics = 0
        };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnBadRequest_WhenQuestionTypeInvalid()
    {
        var service = new Mock<IAdaptiveQuizService>();
        var controller = new AdaptiveQuizzesController(service.Object);

        var request = new AdaptiveQuizRequest
        {
            Mode = AdaptiveQuizMode.Review,
            QuestionCount = 5,
            MaxTopics = 5,
            QuestionType = "InvalidType"
        };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnCreated_WhenSuccess()
    {
        var service = new Mock<IAdaptiveQuizService>();
        var response = new AdaptiveQuizResponse(
            new Cognify.Server.Dtos.PendingQuizDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Quiz", "Note", "Module", "Beginner", "MultipleChoice", 3, "Generating", null, DateTime.UtcNow),
            "Topic",
            0.5,
            0.1);

        service.Setup(s => s.CreateAdaptiveQuizAsync(It.IsAny<AdaptiveQuizRequest>())).ReturnsAsync(response);

        var controller = new AdaptiveQuizzesController(service.Object);
        var request = new AdaptiveQuizRequest { Mode = AdaptiveQuizMode.Review, QuestionCount = 5, MaxTopics = 5 };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnNotFound_WhenNoteMissing()
    {
        var service = new Mock<IAdaptiveQuizService>();
        service.Setup(s => s.CreateAdaptiveQuizAsync(It.IsAny<AdaptiveQuizRequest>())).ThrowsAsync(new KeyNotFoundException());

        var controller = new AdaptiveQuizzesController(service.Object);
        var request = new AdaptiveQuizRequest { Mode = AdaptiveQuizMode.Review, QuestionCount = 5, MaxTopics = 5 };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnForbid_WhenUnauthorized()
    {
        var service = new Mock<IAdaptiveQuizService>();
        service.Setup(s => s.CreateAdaptiveQuizAsync(It.IsAny<AdaptiveQuizRequest>())).ThrowsAsync(new UnauthorizedAccessException());

        var controller = new AdaptiveQuizzesController(service.Object);
        var request = new AdaptiveQuizRequest { Mode = AdaptiveQuizMode.Review, QuestionCount = 5, MaxTopics = 5 };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateAdaptiveQuiz_ShouldReturnBadRequest_WhenInvalidOperation()
    {
        var service = new Mock<IAdaptiveQuizService>();
        service.Setup(s => s.CreateAdaptiveQuizAsync(It.IsAny<AdaptiveQuizRequest>())).ThrowsAsync(new InvalidOperationException("No eligible"));

        var controller = new AdaptiveQuizzesController(service.Object);
        var request = new AdaptiveQuizRequest { Mode = AdaptiveQuizMode.Review, QuestionCount = 5, MaxTopics = 5 };

        var result = await controller.CreateAdaptiveQuiz(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
