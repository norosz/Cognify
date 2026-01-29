using Cognify.Server.Controllers;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class LearningAnalyticsControllerTests
{
    [Fact]
    public async Task GetSummary_ReturnsBadRequest_OnInvalidDays()
    {
        var service = new Mock<ILearningAnalyticsService>();
        var controller = new LearningAnalyticsController(service.Object);

        var result = await controller.GetSummary(0, 14, 10);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSummary_ReturnsOk_OnValidRequest()
    {
        var service = new Mock<ILearningAnalyticsService>();
        var summary = new LearningAnalyticsSummaryDto(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            1,
            80,
            2,
            0.5,
            2,
            0.6,
            0.4,
            [],
            []);

        service.Setup(s => s.GetSummaryAsync(30, 14, 10)).ReturnsAsync(summary);
        var controller = new LearningAnalyticsController(service.Object);

        var result = await controller.GetSummary(30, 14, 10);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(summary);
    }
}
