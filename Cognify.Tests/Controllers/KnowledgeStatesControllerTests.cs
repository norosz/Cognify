using Cognify.Server.Controllers;
using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class KnowledgeStatesControllerTests
{
    [Fact]
    public async Task GetMyStates_ReturnsStates()
    {
        var service = new Mock<IKnowledgeStateService>();
        var states = new List<UserKnowledgeStateDto>
        {
            new UserKnowledgeStateDto { Id = Guid.NewGuid(), Topic = "T", MasteryScore = 0.5, ConfidenceScore = 0.5, ForgettingRisk = 0.3 }
        };

        service.Setup(s => s.GetMyStatesAsync()).ReturnsAsync(states);
        var controller = new KnowledgeStatesController(service.Object);

        var result = await controller.GetMyStates();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(states);
    }

    [Fact]
    public async Task GetReviewQueue_ReturnsItems()
    {
        var service = new Mock<IKnowledgeStateService>();
        var items = new List<ReviewQueueItemDto>
        {
            new ReviewQueueItemDto { Topic = "T", MasteryScore = 0.4, ForgettingRisk = 0.6 }
        };

        service.Setup(s => s.GetReviewQueueAsync(5)).ReturnsAsync(items);
        var controller = new KnowledgeStatesController(service.Object);

        var result = await controller.GetReviewQueue(5);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(items);
    }
}
