using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Cognify.Server.Services.Interfaces;
using Cognify.Server.DTOs;
using System.Text.Json;

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

    public async Task<List<QuestionDto>> GenerateQuestionsFromNoteAsync(string noteContent, int count = 5)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var chatClient = _client.GetChatClient(model);

        var prompt = $$"""
        You are a helpful teaching assistant.
        Generate {{count}} multiple-choice questions based on the provided note content.
        
        The output must be a valid JSON object with a single property "questions" containing an array of question objects.
        Do not wrap the JSON in markdown code blocks.
        
        Each question object must follow this schema:
        {
            "prompt": "Question text",
            "type": "MultipleChoice", 
            "options": ["Option 1", "Option 2", "Option 3", "Option 4"],
            "correctAnswer": "exact text of the correct option",
            "explanation": "Why this is correct"
        }

        Note Content:
        {{noteContent}}
        """;

        try 
        {
            var completion = await chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage("You are a helpful assistant that generates quiz questions in JSON format."),
                    new UserChatMessage(prompt)
                ],
                new ChatCompletionOptions()
                {
                     ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                }
            );

            var json = completion.Value.Content[0].Text;
            
            // Deserialize
            var result = JsonSerializer.Deserialize<QuestionResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Questions ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate questions from AI.");
            throw; // Or return empty list / handle gracefully
        }
    }

    private class QuestionResponse
    {
        public List<QuestionDto> Questions { get; set; } = [];
    }
}
