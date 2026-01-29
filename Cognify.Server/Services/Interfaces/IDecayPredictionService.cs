namespace Cognify.Server.Services.Interfaces;

public interface IDecayPredictionService
{
    double CalculateForgettingRisk(double masteryScore, DateTime? lastReviewedAt, DateTime now);
    double CalculateForgettingRiskAt(double masteryScore, DateTime? lastReviewedAt, DateTime now, DateTime atDate);
    DateTime CalculateNextReviewAt(double masteryScore, DateTime now);
}
