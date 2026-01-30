using Cognify.Server.Dtos.Analytics;

namespace Cognify.Server.Services.Interfaces;

public interface IStatsService
{
    Task<ModuleStatsDto?> GetModuleStatsAsync(Guid moduleId);
    Task<QuizStatsDto?> GetQuizStatsAsync(Guid quizId);
}
