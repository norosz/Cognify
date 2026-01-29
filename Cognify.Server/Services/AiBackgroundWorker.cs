using Cognify.Server.Data;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class AiBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AiBackgroundWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingExtractionsAsync(stoppingToken);
                await ProcessPendingQuizzesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI background worker loop failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingExtractionsAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
        var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IExtractedContentService>();
        var agentRunService = scope.ServiceProvider.GetRequiredService<IAgentRunService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var modelName = config["OpenAI:Model"] ?? "gpt-4o";

        var pendingExtractions = await db.ExtractedContents
            .Include(e => e.Document)
            .Where(e => !e.IsSaved && e.Status == ExtractedContentStatus.Processing)
            .OrderBy(e => e.ExtractedAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var item in pendingExtractions)
        {
            if (item.AgentRunId.HasValue)
            {
                await agentRunService.MarkRunningAsync(item.AgentRunId.Value, modelName);
            }

            if (item.Document == null || string.IsNullOrWhiteSpace(item.Document.BlobPath))
            {
                await extractionService.MarkAsErrorAsync(item.Id, "Document missing or invalid.");
                if (item.AgentRunId.HasValue)
                {
                    await agentRunService.MarkFailedAsync(item.AgentRunId.Value, "Document missing or invalid.");
                }
                continue;
            }

            var contentType = GetContentType(item.Document.FileName);
            if (!contentType.StartsWith("image/"))
            {
                await extractionService.MarkAsErrorAsync(item.Id, "Only image files are currently supported for handwriting extraction.");
                if (item.AgentRunId.HasValue)
                {
                    await agentRunService.MarkFailedAsync(item.AgentRunId.Value, "Only image files are currently supported for handwriting extraction.");
                }
                continue;
            }

            try
            {
                await using var stream = await blobService.DownloadStreamAsync(item.Document.BlobPath);
                var text = await aiService.ParseHandwritingAsync(stream, contentType);
                await extractionService.UpdateAsync(item.Id, text);
                if (item.AgentRunId.HasValue)
                {
                    await agentRunService.MarkCompletedAsync(item.AgentRunId.Value, text);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background extraction failed for ExtractedContent {ExtractedContentId}", item.Id);
                await extractionService.MarkAsErrorAsync(item.Id, "AI Extraction failed. Please try again.");
                if (item.AgentRunId.HasValue)
                {
                    await agentRunService.MarkFailedAsync(item.AgentRunId.Value, "AI Extraction failed. Please try again.");
                }
            }
        }
    }

    private async Task ProcessPendingQuizzesAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
        var pendingService = scope.ServiceProvider.GetRequiredService<IPendingQuizService>();
        var agentRunService = scope.ServiceProvider.GetRequiredService<IAgentRunService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var modelName = config["OpenAI:Model"] ?? "gpt-4o";

        var pendingQuizzes = await db.PendingQuizzes
            .AsNoTracking()
            .Where(p => p.Status == PendingQuizStatus.Generating)
            .OrderBy(p => p.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var quiz in pendingQuizzes)
        {
            try
            {
                if (quiz.AgentRunId.HasValue)
                {
                    await agentRunService.MarkRunningAsync(quiz.AgentRunId.Value, modelName);
                }

                var note = await db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == quiz.NoteId, stoppingToken);
                if (note == null || string.IsNullOrWhiteSpace(note.Content))
                {
                    await pendingService.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Failed, errorMessage: "Note content not found.");
                    if (quiz.AgentRunId.HasValue)
                    {
                        await agentRunService.MarkFailedAsync(quiz.AgentRunId.Value, "Note content not found.");
                    }
                    continue;
                }

                var questions = await aiService.GenerateQuestionsAsync(
                    note.Content,
                    (QuestionType)quiz.QuestionType,
                    (int)quiz.Difficulty,
                    quiz.QuestionCount);

                if (questions == null || questions.Count == 0)
                {
                    await pendingService.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Failed, errorMessage: "AI failed to generate any questions. Please try again with more detailed content.");
                    if (quiz.AgentRunId.HasValue)
                    {
                        await agentRunService.MarkFailedAsync(quiz.AgentRunId.Value, "AI failed to generate any questions. Please try again with more detailed content.");
                    }
                    continue;
                }

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                var questionsJson = System.Text.Json.JsonSerializer.Serialize(questions, options);
                await pendingService.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Ready, questionsJson: questionsJson, actualQuestionCount: questions.Count);
                if (quiz.AgentRunId.HasValue)
                {
                    await agentRunService.MarkCompletedAsync(quiz.AgentRunId.Value, questionsJson);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background quiz generation failed for PendingQuiz {PendingQuizId}", quiz.Id);
                await pendingService.UpdateStatusAsync(quiz.Id, PendingQuizStatus.Failed, errorMessage: "AI quiz generation failed. Please try again.");
                if (quiz.AgentRunId.HasValue)
                {
                    await agentRunService.MarkFailedAsync(quiz.AgentRunId.Value, "AI quiz generation failed. Please try again.");
                }
            }
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}