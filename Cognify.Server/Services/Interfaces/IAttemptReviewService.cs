using Cognify.Server.Dtos.Attempt;

namespace Cognify.Server.Services.Interfaces;

public interface IAttemptReviewService
{
    Task<AttemptReviewDto?> GetAttemptReviewAsync(Guid quizId, Guid attemptId);
    Task<AttemptReviewDto?> GetExamAttemptReviewAsync(Guid moduleId, Guid examAttemptId);
}
