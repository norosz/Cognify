using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Cognify.Server.Services.Interfaces;
using Cognify.Server.Models.Ai;
using Cognify.Server.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        var difficultyPrompt = difficulty switch
        {
            1 => "Beginner level: Focus on basic definitions and recall.",
            2 => "Intermediate level: Application of concepts and detailed understanding.",
            3 => "Advanced level: Complex synthesis, edge cases, and deep analysis.",
            _ => "Standard level."
        };

        var typePrompt = type switch
        {
            QuestionType.MultipleChoice => """
                Type: MultipleChoice.
                Schema: {"text": "question", "options": ["A", "B", "C", "D"], "correctAnswer": "Exact Option Text", "explanation": "reason"}
                """,
            QuestionType.TrueFalse => """
                Type: TrueFalse.
                Schema: {"text": "Statement", "options": ["True", "False"], "correctAnswer": "True/False", "explanation": "reason"}
                """,
            QuestionType.OpenText => """
                Type: OpenText.
                Schema: {"text": "Open ended question", "explanation": "Key points expected in the answer"}
                """,
            QuestionType.Matching => """
                Type: Matching.
                Schema: {"text": "Match the following terms", "pairs": ["Term1:Definition1", "Term2:Definition2"], "explanation": "Context"}
                """,
            _ => "Type: MultipleChoice"
        };

        var prompt = $$"""
        You are an expert tutor.
        Generate {{count}} questions based on the provided content.
        
        Difficulty: {{difficultyPrompt}}
        {{typePrompt}}

        The output must be a valid JSON object with a single property "questions" containing an array of objects matching the schema above.
        Each object must strictly follow the schema and include the "type" property set to "{{type}}".
        
        Content:
        {{content}}
        """;

        try 
        {
            var completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are a helper that outputs strict JSON."),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions()
                {
                     ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                }
            );

            var json = completion.Value.Content[0].Text;
            var result = JsonSerializer.Deserialize<AssessmentBundle>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });
            return result?.Questions ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate questions.");
            return [];
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
            ChatMessageContentPart.CreateTextPart("Please transcribe this handwritten note into clear, formatted Markdown text. Do not add conversational filler, just the content."),
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

    public async Task<string> GradeAnswerAsync(string question, string answer, string context)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        var prompt = $$"""
        Question: {{question}}
        Student Answer: {{answer}}
        Context/Correct Answer Key: {{context}}

        Evaluate the student's answer. Provide a constructive feedback and a score (0-100).
        Format: "Score: [0-100]\n\nFeedback: [Your feedback here]"
        """;

        var completion = await chatClient.CompleteChatAsync([new UserChatMessage(prompt)]);
        return completion.Value.Content[0].Text;
    }
}
