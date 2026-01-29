using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IAgentRunService
{
    Task<AgentRun> CreateAsync(Guid userId, AgentRunType type, string inputHash, string? correlationId = null, string? promptVersion = null);
    Task MarkRunningAsync(Guid runId, string? model = null);
    Task MarkCompletedAsync(Guid runId, string? outputJson, int? promptTokens = null, int? completionTokens = null, int? totalTokens = null);
    Task MarkFailedAsync(Guid runId, string errorMessage);
}