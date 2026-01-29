using System.Security.Cryptography;
using System.Text;
using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class AgentRunService(ApplicationDbContext db) : IAgentRunService
{
    public async Task<AgentRun> CreateAsync(Guid userId, AgentRunType type, string inputHash, string? correlationId = null, string? promptVersion = null)
    {
        var run = new AgentRun
        {
            UserId = userId,
            Type = type,
            Status = AgentRunStatus.Pending,
            InputHash = inputHash,
            CorrelationId = correlationId,
            PromptVersion = promptVersion
        };

        db.AgentRuns.Add(run);
        await db.SaveChangesAsync();
        return run;
    }

    public async Task MarkRunningAsync(Guid runId, string? model = null)
    {
        var run = await db.AgentRuns.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return;

        run.Status = AgentRunStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(model))
        {
            run.Model = model;
        }

        await db.SaveChangesAsync();
    }

    public async Task MarkCompletedAsync(Guid runId, string? outputJson, int? promptTokens = null, int? completionTokens = null, int? totalTokens = null)
    {
        var run = await db.AgentRuns.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return;

        run.Status = AgentRunStatus.Completed;
        run.CompletedAt = DateTime.UtcNow;
        run.OutputJson = outputJson;
        run.PromptTokens = promptTokens;
        run.CompletionTokens = completionTokens;
        run.TotalTokens = totalTokens;

        await db.SaveChangesAsync();
    }

    public async Task MarkFailedAsync(Guid runId, string errorMessage)
    {
        var run = await db.AgentRuns.FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null) return;

        run.Status = AgentRunStatus.Failed;
        run.CompletedAt = DateTime.UtcNow;
        run.ErrorMessage = errorMessage;

        await db.SaveChangesAsync();
    }

    public static string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}