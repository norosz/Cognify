using Cognify.Server.Controllers;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class LearningAnalyticsControllerTests
{
    [Fact]
    public async Task GetTrends_ReturnsBadRequest_OnInvalidBucketDays()
    {
        var service = new Mock<ILearningAnalyticsService>();
        var controller = new LearningAnalyticsController(service.Object);

        var result = await controller.GetTrends(from: null, to: null, bucketDays: 0);

        var problem = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        problem.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task GetSummary_ReturnsOk()
    {
        var service = new Mock<ILearningAnalyticsService>();
        var summary = new LearningAnalyticsSummaryDto
        {
            TotalTopics = 3,
            AverageMastery = 0.6,
            AverageForgettingRisk = 0.4,
            WeakTopicsCount = 1,
            TotalAttempts = 2,
            AccuracyRate = 0.75,
            ExamReadinessScore = 0.65,
            LearningVelocity = 0.5,
            LastActivityAt = DateTime.UtcNow
        };

        service.Setup(s => s.GetSummaryAsync()).ReturnsAsync(summary);
        var controller = new LearningAnalyticsController(service.Object);

        var result = await controller.GetSummary();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(summary);
    }
}
