using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai;
using Cognify.Server.Services.Interfaces;
using OpenAI;
using OpenAI.Chat;

namespace Cognify.Server.Services;

public class AiService : IAiService
{
    private readonly OpenAIClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiService> _logger;

    public AiService(OpenAIClient client, IConfiguration configuration, ILogger<AiService> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<GeneratedQuestion>> GenerateQuestionsAsync(string content, QuestionType type, int difficulty, int count)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        var difficultyPrompt = AiPrompts.GetDifficultySystemPrompt((int)difficulty);
        var typePrompt = AiPrompts.GetTypeSystemPrompt(type);
        var prompt = AiPrompts.BuildGenerationPrompt(count, difficulty, difficultyPrompt, typePrompt, content);

        try 
        {
            var completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are a strict JSON generator. You always wrap your response in a 'questions' array inside a root object."),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions()
                {
                     ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                }
            );

            var json = completion.Value.Content[0].Text;
            _logger.LogInformation("AI Response received: {Json}", json);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Resilience: Handle both {"questions": [...]} and [...] with case-insensitivity
            JsonElement questionsArray = default;
            bool found = false;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "questions", StringComparison.OrdinalIgnoreCase))
                    {
                        questionsArray = prop.Value;
                        found = true;
                        break;
                    }
                }

                // Fallback: If no "questions" property, take the first array property found
                if (!found)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            questionsArray = prop.Value;
                            found = true;
                            break;
                        }
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                questionsArray = root;
                found = true;
            }

            if (!found || questionsArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("AI response did not contain a valid questions array. Root: {Kind}", root.ValueKind);
                return [];
            }

            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true, 
                Converters = { new JsonStringEnumConverter() } 
            };

            var questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(questionsArray.GetRawText(), options);
            return questions ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate or parse questions. JSON might be malformed.");
            return [];
        }
    }

    public async Task<QuizGenerationContractResponse> GenerateQuizAsync(QuizGenerationContractRequest request)
    {
        var questions = await GenerateQuestionsAsync(
            request.NoteContent,
            request.QuestionType,
            request.Difficulty,
            request.QuestionCount);

        return new QuizGenerationContractResponse(
            request.ContractVersion,
            questions,
            QuizRubric: null);
    }

    public async Task<string> ParseHandwritingAsync(Stream imageStream, string contentType)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        var message = new UserChatMessage(
            ChatMessageContentPart.CreateTextPart("""
                Please transcribe this handwritten note into clear, formatted Markdown text. 
                
                RULES:
                1. Always use LaTeX for mathematical formulas or variables (e.g., $x=2$, $\int f(x)dx$).
                2. Use Markdown formatting (**bold**, *italic*, headers, lists).
                3. Use Markdown backticks for code-like identifiers (`identifier`).
                4. Do not add conversational filler, just the content.
                """),
            ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), contentType)
        );

        try
        {
            var completion = await chatClient.CompleteChatAsync([message]);
            return completion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse handwriting.");
            throw;
        }
    }

    public async Task<OcrContractResponse> ParseHandwritingAsync(Stream imageStream, OcrContractRequest request)
    {
        var extracted = await ParseHandwritingAsync(imageStream, request.ContentType);
        return new OcrContractResponse(request.ContractVersion, extracted, BlocksJson: null, Confidence: null);
    }

    public async Task<string> GradeAnswerAsync(string question, string answer, string context)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        var prompt = $$"""
        Question: {{question}}
        Student Answer: {{answer}}
        Context/Correct Answer Key: {{context}}

        Evaluate the student's answer based on the context.
        Provide constructive feedback and a score (0-100).
        
        FORMATTING RULES:
        1. Use LaTeX for math/formulas ($...$).
        2. Use Markdown for emphasis and structure.
        3. Format: "Score: [0-100]\n\nFeedback: [Your feedback here]"
        """;

        var completion = await chatClient.CompleteChatAsync([new UserChatMessage(prompt)]);
        return completion.Value.Content[0].Text;
    }

    public async Task<GradingContractResponse> GradeAnswerAsync(GradingContractRequest request)
    {
        var context = request.AnswerKey;
        if (!string.IsNullOrWhiteSpace(request.Rubric))
        {
            context = $"{context}\n\nRubric:\n{request.Rubric}";
        }

        var analysis = await GradeAnswerAsync(request.QuestionPrompt, request.UserAnswer, context);
        var score = TryParseScore(analysis);
        var feedback = TryParseFeedback(analysis);

        return new GradingContractResponse(
            request.ContractVersion,
            score ?? 0,
            100,
            feedback,
            DetectedMistakes: null,
            ConfidenceEstimate: null,
            RawAnalysis: analysis);
    }

    private static double? TryParseScore(string analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis)) return null;

        var match = Regex.Match(analysis, @"Score\s*:\s*(\d{1,3})", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Clamp(score, 0, 100);
        }

        return null;
    }

    private static string? TryParseFeedback(string analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis)) return null;

        var match = Regex.Match(analysis, @"Feedback\s*:\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
