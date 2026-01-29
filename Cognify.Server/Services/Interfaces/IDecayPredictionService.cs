namespace Cognify.Server.Services.Interfaces;

public interface IDecayPredictionService
{
    (DateTime NextReviewAt, double ForgettingRisk) Predict(
        double masteryScore,
        double confidenceScore,
        DateTime now,
        DateTime? lastReviewedAt,
        int incorrectCount);
}
