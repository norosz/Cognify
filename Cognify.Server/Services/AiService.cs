using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Cognify.Server.Dtos.Ai;
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
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        var difficultyPrompt = AiPrompts.GetDifficultySystemPrompt(request.Difficulty);
        var typePrompt = AiPrompts.GetTypeSystemPrompt(request.QuestionType);
        var prompt = AiPrompts.BuildGenerationPromptWithRubric(
            request.QuestionCount,
            request.Difficulty,
            difficultyPrompt,
            typePrompt,
            request.NoteContent,
            request.KnowledgeStateSnapshot,
            request.MistakeFocus);

        try
        {
            var completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are a strict JSON generator. You always wrap your response in a 'questions' array inside a root object and include a 'quizRubric'."),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                }
            );

            var json = completion.Value.Content[0].Text;
            _logger.LogInformation("AI Response received: {Json}", json);

            var parsed = ParseQuizGenerationResponse(json);
            if (parsed.Questions.Count == 0)
            {
                _logger.LogWarning("AI response did not contain a valid questions array.");
            }

            return new QuizGenerationContractResponse(
                request.ContractVersion,
                parsed.Questions,
                parsed.QuizRubric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate or parse quiz with rubric. Falling back to questions-only generation.");
            var fallback = await GenerateQuestionsAsync(
                request.NoteContent,
                request.QuestionType,
                request.Difficulty,
                request.QuestionCount);

            return new QuizGenerationContractResponse(request.ContractVersion, fallback, QuizRubric: null);
        }
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

        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);
        var prompt = AiPrompts.BuildGradingPromptWithRubric(
            request.QuestionPrompt,
            request.UserAnswer,
            context,
            request.KnownMistakePatterns);

        try
        {
            var completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are a strict JSON generator. Return only the required grading JSON object."),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                }
            );

            var json = completion.Value.Content[0].Text;
            var parsed = ParseGradingResponse(json);

            return new GradingContractResponse(
                request.ContractVersion,
                parsed.Score,
                100,
                parsed.Feedback,
                parsed.DetectedMistakes,
                parsed.ConfidenceEstimate,
                RawAnalysis: json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse grading JSON. Falling back to legacy grading.");
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

    private static (double Score, string? Feedback, IReadOnlyList<string>? DetectedMistakes, double? ConfidenceEstimate) ParseGradingResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (0, null, null, null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (0, null, null, null);
            }

            double score = 0;
            string? feedback = null;
            double? confidence = null;
            IReadOnlyList<string>? mistakes = null;

            if (root.TryGetProperty("score", out var scoreProp) && scoreProp.ValueKind == JsonValueKind.Number)
            {
                score = Math.Clamp(scoreProp.GetDouble(), 0, 100);
            }

            if (root.TryGetProperty("feedback", out var feedbackProp) && feedbackProp.ValueKind == JsonValueKind.String)
            {
                feedback = feedbackProp.GetString();
            }

            if (root.TryGetProperty("confidenceEstimate", out var confProp) && confProp.ValueKind == JsonValueKind.Number)
            {
                confidence = Math.Clamp(confProp.GetDouble(), 0, 1);
            }

            if (root.TryGetProperty("detectedMistakes", out var mistakesProp) && mistakesProp.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in mistakesProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            list.Add(value);
                        }
                    }
                }

                mistakes = list.Count > 0 ? list : null;
            }

            return (score, feedback, mistakes, confidence);
        }
        catch
        {
            return (0, null, null, null);
        }
    }

    private static (List<GeneratedQuestion> Questions, string? QuizRubric) ParseQuizGenerationResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return ([], null);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            List<GeneratedQuestion> questions = [];
            string? rubric = null;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "questions", StringComparison.OrdinalIgnoreCase)
                        && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(prop.Value.GetRawText(), options) ?? [];
                    }
                    else if (string.Equals(prop.Name, "quizRubric", StringComparison.OrdinalIgnoreCase))
                    {
                        rubric = prop.Value.ValueKind == JsonValueKind.String
                            ? prop.Value.GetString()
                            : prop.Value.GetRawText();
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(root.GetRawText(), options) ?? [];
            }

            return (questions, rubric);
        }
        catch
        {
            return ([], null);
        }
    }

    public async Task<ExplainMistakeResponse> ExplainMistakeAsync(ExplainMistakeRequest request)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        var mistakeList = request.DetectedMistakes.Count > 0
            ? string.Join(", ", request.DetectedMistakes)
            : "(none provided)";

        var prompt = $$"""
        You are a helpful tutor. Provide a clear explanation for the student's mistake.
        Return JSON only with these fields:
        - explanationMarkdown (string)
        - keyTakeaways (array of short strings)
        - nextSteps (array of short strings)

        Question: {{request.QuestionPrompt}}
        Student Answer: {{request.UserAnswer}}
        Correct Answer: {{request.CorrectAnswer}}
        Detected Mistakes: {{mistakeList}}
        Concept Label: {{request.ConceptLabel ?? "(none)"}}
        Module Context: {{request.ModuleContext ?? "(none)"}}
        Note Context: {{request.NoteContext ?? "(none)"}}

        Rules:
        - Use Markdown in explanationMarkdown.
        - Use KaTeX for formulas ($...$).
        - Keep explanation concise and constructive.
        """;

        try
        {
            var completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("Return a strict JSON object with explanationMarkdown, keyTakeaways, nextSteps."),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                });

            var json = completion.Value.Content[0].Text;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<ExplainMistakeResponse>(json, options);
            if (parsed == null)
            {
                return new ExplainMistakeResponse
                {
                    ExplanationMarkdown = "No explanation available.",
                    KeyTakeaways = [],
                    NextSteps = []
                };
            }

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate mistake explanation");
            return new ExplainMistakeResponse
            {
                ExplanationMarkdown = "We could not generate an explanation at this time.",
                KeyTakeaways = [],
                NextSteps = []
            };
        }
    }
}
