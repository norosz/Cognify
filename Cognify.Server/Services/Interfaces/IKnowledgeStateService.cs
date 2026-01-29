using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IKnowledgeStateService
{
    Task ApplyAttemptResultAsync(Attempt attempt, QuestionSet questionSet, IReadOnlyCollection<KnowledgeInteractionInput> interactions);
    Task<List<UserKnowledgeStateDto>> GetMyStatesAsync();
    Task<List<ReviewQueueItemDto>> GetReviewQueueAsync(int maxItems = 10);
}
