using Cognify.Server.Services;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Services;

public class DecayPredictionServiceTests
{
    [Fact]
    public void CalculateNextReviewAt_ShouldIncreaseInterval_WithHigherMastery()
    {
        var service = new DecayPredictionService();
        var now = DateTime.UtcNow;

        var low = service.CalculateNextReviewAt(0.2, now);
        var high = service.CalculateNextReviewAt(0.9, now);

        high.Should().BeAfter(low);
    }

    [Fact]
    public void CalculateForgettingRisk_ShouldIncreaseOverTime()
    {
        var service = new DecayPredictionService();
        var now = DateTime.UtcNow;
        var lastReviewedAt = now.AddDays(-1);

        var baseline = service.CalculateForgettingRiskAt(0.6, lastReviewedAt, now, now);
        var later = service.CalculateForgettingRiskAt(0.6, lastReviewedAt, now, now.AddDays(7));

        later.Should().BeGreaterThan(baseline);
    }

    [Fact]
    public void CalculateForgettingRisk_ShouldDecrease_WithHigherMastery()
    {
        var service = new DecayPredictionService();
        var now = DateTime.UtcNow;
        var lastReviewedAt = now.AddDays(-3);

        var low = service.CalculateForgettingRisk(0.2, lastReviewedAt, now);
        var high = service.CalculateForgettingRisk(0.9, lastReviewedAt, now);

        high.Should().BeLessThan(low);
    }
}
