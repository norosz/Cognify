using Cognify.Server.Services;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Services;

public class DecayPredictionServiceTests
{
    [Fact]
    public void Predict_ShouldIncreaseInterval_WithHigherMastery()
    {
        var service = new DecayPredictionService();
        var now = DateTime.UtcNow;

        var low = service.Predict(0.2, 0.2, now, now.AddDays(-1), incorrectCount: 0);
        var high = service.Predict(0.9, 0.9, now, now.AddDays(-1), incorrectCount: 0);

        high.NextReviewAt.Should().BeAfter(low.NextReviewAt);
        high.ForgettingRisk.Should().BeLessThanOrEqualTo(low.ForgettingRisk);
    }

    [Fact]
    public void Predict_ShouldIncreaseRisk_WithIncorrectCount()
    {
        var service = new DecayPredictionService();
        var now = DateTime.UtcNow;

        var baseResult = service.Predict(0.6, 0.6, now, now.AddDays(-2), incorrectCount: 0);
        var withErrors = service.Predict(0.6, 0.6, now, now.AddDays(-2), incorrectCount: 3);

        withErrors.ForgettingRisk.Should().BeGreaterThanOrEqualTo(baseResult.ForgettingRisk);
        withErrors.NextReviewAt.Should().BeOnOrBefore(baseResult.NextReviewAt);
    }
}
