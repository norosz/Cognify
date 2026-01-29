using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class DecayPredictionService : IDecayPredictionService
{
    private const double MinScore = 0.0;
    private const double MaxScore = 1.0;

    public double CalculateForgettingRisk(double masteryScore, DateTime? lastReviewedAt, DateTime now)
    {
        return CalculateForgettingRiskAt(masteryScore, lastReviewedAt, now, now);
    }

    public double CalculateForgettingRiskAt(double masteryScore, DateTime? lastReviewedAt, DateTime now, DateTime atDate)
    {
        var mastery = Clamp(masteryScore);
        var baselineDate = lastReviewedAt ?? now;
        var days = Math.Max(0, (atDate - baselineDate).TotalDays);
        var decayRate = 0.12 + (1 - mastery) * 0.25;
        var retention = mastery * Math.Exp(-decayRate * days);
        var risk = 1 - retention;
        return Clamp(risk);
    }

    public DateTime CalculateNextReviewAt(double masteryScore, DateTime now)
    {
        var mastery = Clamp(masteryScore);

        if (mastery >= 0.85)
        {
            return now.AddDays(14);
        }

        if (mastery >= 0.7)
        {
            return now.AddDays(7);
        }

        if (mastery >= 0.5)
        {
            return now.AddDays(3);
        }

        return now.AddDays(1);
    }

    private static double Clamp(double value)
    {
        return Math.Max(MinScore, Math.Min(MaxScore, value));
    }
}
