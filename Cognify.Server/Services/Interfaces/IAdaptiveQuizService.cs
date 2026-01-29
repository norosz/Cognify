using Cognify.Server.Dtos.Adaptive;

namespace Cognify.Server.Services.Interfaces;

public interface IAdaptiveQuizService
{
    Task<AdaptiveQuizResponse> CreateAdaptiveQuizAsync(AdaptiveQuizRequest request);
}
