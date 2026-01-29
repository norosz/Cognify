using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class DecayPredictionService : IDecayPredictionService
{
    private const double MinRisk = 0.05;
    private const double MaxRisk = 0.95;
    private const double MinIntervalDays = 1;
    private const double MaxIntervalDays = 30;

    public (DateTime NextReviewAt, double ForgettingRisk) Predict(
        double masteryScore,
        double confidenceScore,
        DateTime now,
        DateTime? lastReviewedAt,
        int incorrectCount)
    {
        var stability = 1 + (masteryScore * 10) + (confidenceScore * 4);
        var penalty = Math.Min(incorrectCount, 5) * 0.6;
        var interval = Math.Clamp(stability - penalty, MinIntervalDays, MaxIntervalDays);

        var daysSince = lastReviewedAt.HasValue
            ? Math.Max(0, (now - lastReviewedAt.Value).TotalDays)
            : interval / 2;

        var rawRisk = daysSince / (interval + 1);
        var adjusted = rawRisk + (incorrectCount * 0.03);
        var risk = Math.Clamp(adjusted, MinRisk, MaxRisk);

        return (now.AddDays(interval), risk);
    }
}
