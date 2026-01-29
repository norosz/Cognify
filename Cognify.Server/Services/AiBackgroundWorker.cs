using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Ai.Contracts;
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
    private const int MaxOcrImages = 10;

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
        var pdfTextExtractor = scope.ServiceProvider.GetRequiredService<IPdfTextExtractor>();
        var pdfImageExtractor = scope.ServiceProvider.GetRequiredService<IPdfImageExtractor>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IExtractedContentService>();
        var agentRunService = scope.ServiceProvider.GetRequiredService<IAgentRunService>();
        var materialService = scope.ServiceProvider.GetRequiredService<IMaterialService>();
        var materialExtractionService = scope.ServiceProvider.GetRequiredService<IMaterialExtractionService>();
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
            try
            {
                await using var stream = await blobService.DownloadStreamAsync(item.Document.BlobPath);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, stoppingToken);
                buffer.Position = 0;

                string text;
                List<PdfImageMetadata> images = [];
                string? blocksJson = "[]";
                double? confidence = null;
                var usedOcr = false;

                if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    text = await pdfTextExtractor.ExtractTextAsync(buffer, stoppingToken);

                    buffer.Position = 0;
                    var extractedImages = await pdfImageExtractor.ExtractImagesAsync(buffer, stoppingToken);

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        text = await OcrPdfImagesAsync(aiService, extractedImages, stoppingToken);
                        usedOcr = true;
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        await extractionService.MarkAsErrorAsync(item.Id, "No selectable text or images found in PDF.");
                        if (item.AgentRunId.HasValue)
                        {
                            await agentRunService.MarkFailedAsync(item.AgentRunId.Value, "No selectable text or images found in PDF.");
                        }
                        continue;
                    }

                    images = await UploadPdfImagesAsync(blobService, item.Document.Id, extractedImages, stoppingToken);

                    if (images.Count > 0)
                    {
                        text = AppendImagesToMarkdown(text, images);
                    }

                    confidence = usedOcr ? 0.6 : 0.95;
                }
                else if (contentType.StartsWith("text/") || contentType == "application/xhtml+xml")
                {
                    buffer.Position = 0;
                    text = await ReadTextAsync(contentType, buffer, stoppingToken);
                    confidence = 0.9;
                }
                else if (IsOfficeContent(contentType))
                {
                    buffer.Position = 0;
                    text = ExtractOfficeText(contentType, buffer);
                    confidence = 0.85;
                }
                else if (contentType.StartsWith("image/"))
                {
                    buffer.Position = 0;
                    var ocrRequest = new OcrContractRequest(
                        AgentContractVersions.V2,
                        contentType,
                        Language: null,
                        Hints: null);
                    var ocrResponse = await aiService.ParseHandwritingAsync(buffer, ocrRequest);
                    text = ocrResponse.ExtractedText;
                    blocksJson = ocrResponse.BlocksJson ?? blocksJson;
                    confidence = ocrResponse.Confidence ?? 0.6;
                }
                else
                {
                    await extractionService.MarkAsErrorAsync(item.Id, "Unsupported file type for extraction.");
                    if (item.AgentRunId.HasValue)
                    {
                        await agentRunService.MarkFailedAsync(item.AgentRunId.Value, "Unsupported file type for extraction.");
                    }
                    continue;
                }

                await extractionService.UpdateAsync(item.Id, text);

                var material = await materialService.EnsureForDocumentAsync(item.DocumentId, item.UserId);
                var serializerOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                var imagesJson = images.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(images, serializerOptions)
                    : null;
                await materialExtractionService.UpsertExtractionAsync(material, text, imagesJson, blocksJson, confidence);
                if (item.AgentRunId.HasValue)
                {
                    var outputJson = System.Text.Json.JsonSerializer.Serialize(new ExtractionOutput(text, images, blocksJson, confidence), serializerOptions);
                    await agentRunService.MarkCompletedAsync(item.AgentRunId.Value, outputJson);
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

                var adaptiveContext = await LoadAdaptiveContextAsync(db, quiz.UserId, quiz.NoteId, stoppingToken);
                var request = new QuizGenerationContractRequest(
                    AgentContractVersions.V2,
                    BuildAdaptiveContent(note.Content, adaptiveContext),
                    (QuestionType)quiz.QuestionType,
                    (int)quiz.Difficulty,
                    quiz.QuestionCount,
                    adaptiveContext,
                    MistakeFocus: null);

                var quizResponse = await aiService.GenerateQuizAsync(request);
                var questions = quizResponse.Questions;

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
                var questionsJson = System.Text.Json.JsonSerializer.Serialize(quizResponse, options);
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
            ".txt" => "text/plain",
            ".json" => "text/plain",
            ".yaml" => "text/plain",
            ".yml" => "text/plain",
            ".md" => "text/markdown",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".mhtml" => "text/html",
            ".epub" => "application/epub+zip",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private static bool IsOfficeContent(string contentType)
    {
        return contentType is "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            or "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            or "application/epub+zip";
    }

    private static async Task<string> ReadTextAsync(string contentType, Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(cancellationToken);

        if (contentType == "text/html")
        {
            return NormalizeWhitespace(StripHtml(raw));
        }

        return NormalizeWhitespace(raw);
    }

    private static string ExtractOfficeText(string contentType, Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        return contentType switch
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" =>
                NormalizeWhitespace(ReadZipEntryText(archive, "word/document.xml")),
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" =>
                NormalizeWhitespace(ReadZipEntriesText(archive, "ppt/slides/", ".xml")),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" =>
                NormalizeWhitespace(ReadZipEntryText(archive, "xl/sharedStrings.xml")),
            "application/epub+zip" =>
                NormalizeWhitespace(ReadZipEntriesText(archive, string.Empty, ".xhtml", ".html")),
            _ => string.Empty
        };
    }

    private static string ReadZipEntryText(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry == null) return string.Empty;

        using var reader = new StreamReader(entry.Open());
        var xml = reader.ReadToEnd();
        return StripHtml(xml);
    }

    private static string ReadZipEntriesText(ZipArchive archive, string prefix, params string[] extensions)
    {
        var builder = new StringBuilder();
        var entries = archive.Entries
            .Where(e => e.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && extensions.Any(ext => e.FullName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(e => e.FullName)
            .ToList();

        foreach (var entry in entries)
        {
            using var reader = new StreamReader(entry.Open());
            var xml = reader.ReadToEnd();
            var text = StripHtml(xml);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine().AppendLine("---").AppendLine();
                }
                builder.Append(text);
            }
        }

        return builder.ToString();
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var noTags = Regex.Replace(input, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(noTags);
    }

    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var normalized = Regex.Replace(input, "\\s+", " ").Trim();
        return normalized;
    }

    private static string AppendImagesToMarkdown(string text, List<PdfImageMetadata> images)
    {
        if (images.Count == 0) return text;

        var builder = new System.Text.StringBuilder(text.TrimEnd());
        builder.AppendLine().AppendLine();
        builder.AppendLine("## Embedded Images");

        foreach (var image in images)
        {
            builder.AppendLine($"- {image.FileName} (page {image.PageNumber})");
        }

        return builder.ToString();
    }

    private static async Task<List<PdfImageMetadata>> UploadPdfImagesAsync(
        IBlobStorageService blobService,
        Guid documentId,
        List<PdfEmbeddedImage> images,
        CancellationToken cancellationToken)
    {
        var results = new List<PdfImageMetadata>();
        var index = 0;

        foreach (var image in images)
        {
            index++;
            if (image.Bytes == null || image.Bytes.Length == 0) continue;

            var imageId = Guid.NewGuid().ToString("N");
            var extension = image.ContentType == "image/png" ? "png" : "bin";
            var fileName = $"image-{index}.{extension}";
            var blobPath = $"extracted/{documentId}/images/{imageId}.{extension}";

            await using var imageStream = new MemoryStream(image.Bytes);
            await blobService.UploadAsync(blobPath, imageStream, image.ContentType, cancellationToken);

            results.Add(new PdfImageMetadata(
                imageId,
                blobPath,
                fileName,
                image.PageNumber,
                image.Width,
                image.Height
            ));
        }

        return results;
    }

    private static string BuildAdaptiveContent(string baseContent, string? adaptiveContext)
    {
        if (string.IsNullOrWhiteSpace(adaptiveContext))
        {
            return baseContent;
        }

        return $$"""
        {{baseContent}}

        ---
        Adaptive Guidance:
        {{adaptiveContext}}

        Instruction: Emphasize weak areas, common mistakes, and review gaps. Focus on clarity and targeted practice.
        """;
    }

    private static async Task<string?> LoadAdaptiveContextAsync(
        ApplicationDbContext db,
        Guid userId,
        Guid noteId,
        CancellationToken stoppingToken)
    {
        var state = await db.UserKnowledgeStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SourceNoteId == noteId, stoppingToken);

        if (state == null)
        {
            return null;
        }

        var mistakes = ParseMistakePatterns(state.MistakePatternsJson)
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => $"{kv.Key} ({kv.Value})")
            .ToList();

        var mistakeSummary = mistakes.Count > 0
            ? string.Join(", ", mistakes)
            : "None recorded";

        return $$"""
        Topic: {{state.Topic}}
        MasteryScore: {{state.MasteryScore:0.00}}
        ForgettingRisk: {{state.ForgettingRisk:0.00}}
        ConfidenceScore: {{state.ConfidenceScore:0.00}}
        MistakePatterns: {{mistakeSummary}}
        """.Trim();
    }

    private static Dictionary<string, int> ParseMistakePatterns(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    private static async Task<string> OcrPdfImagesAsync(
        IAiService aiService,
        List<PdfEmbeddedImage> images,
        CancellationToken cancellationToken)
    {
        if (images.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var processed = 0;

        foreach (var image in images)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (image.Bytes == null || image.Bytes.Length == 0) continue;

            processed++;
            if (processed > MaxOcrImages) break;

            var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/png" : image.ContentType;
            await using var ms = new MemoryStream(image.Bytes);
            var request = new OcrContractRequest(
                AgentContractVersions.V2,
                contentType,
                Language: null,
                Hints: null);
            var response = await aiService.ParseHandwritingAsync(ms, request);
            var extracted = response.ExtractedText;

            if (string.IsNullOrWhiteSpace(extracted)) continue;

            if (builder.Length > 0)
            {
                builder.AppendLine().AppendLine("---").AppendLine();
            }

            builder.Append(extracted.Trim());
        }

        return builder.ToString();
    }

    private record ExtractionOutput(string Text, List<PdfImageMetadata> Images, string? BlocksJson, double? Confidence);

    private record PdfImageMetadata(
        string Id,
        string BlobPath,
        string FileName,
        int PageNumber,
        int? Width,
        int? Height
    );
}