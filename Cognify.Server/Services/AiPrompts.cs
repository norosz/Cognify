using Cognify.Server.Models;

namespace Cognify.Server.Services;

public static class AiPrompts
{
    public static string GetDifficultySystemPrompt(int difficulty)
    {
        return difficulty switch
        {
            1 => "Beginner level: Focus on basic definitions and recall.",
            2 => "Intermediate level: Application of concepts and detailed understanding.",
            3 => "Advanced level: Complex synthesis, edge cases, and deep analysis.",
            _ => "Standard level."
        };
    }

    public static string GetTypeSystemPrompt(QuestionType type)
    {
        return type switch
        {
            QuestionType.MultipleChoice => """
                Type: MultipleChoice.
                Schema: {"text": "question", "type": "MultipleChoice", "options": ["A", "B", "C", "D"], "correctAnswer": "Exact Option Text", "explanation": "reason"}
                """,
            QuestionType.TrueFalse => """
                Type: TrueFalse.
                Schema: {"text": "Statement", "type": "TrueFalse", "options": ["True", "False"], "correctAnswer": "True/False", "explanation": "reason"}
                """,
            QuestionType.OpenText => """
                Type: OpenText.
                Schema: {"text": "Open ended question", "type": "OpenText", "explanation": "Key points expected in the answer", "correctAnswer": "The reference answer key"}
                """,
            QuestionType.Matching => """
                Type: Matching.
                Schema: {"text": "Match the following terms", "type": "Matching", "pairs": ["Term1:Definition1", "Term2:Definition2"], "explanation": "Context", "correctAnswer": "Term1:Definition1|Term2:Definition2"}
                """,
            QuestionType.Ordering => """
                Type: Ordering.
                Schema: {"text": "Put these events in chronological order", "type": "Ordering", "options": ["Event 1", "Event 2", "Event 3"], "correctAnswer": "Event 1|Event 2|Event 3", "explanation": "Sequence logic"}
                """,
            QuestionType.Mixed => """
                Type: Mixed. Use a variety of MultipleChoice, TrueFalse, Matching, and Ordering.
                Schema: Use the appropriate schema for each type. Ensure "type" property reflects the chosen type.
                """,
            _ => """
                Type: MultipleChoice.
                Schema: {"text": "question", "type": "MultipleChoice", "options": ["A", "B", "C", "D"], "correctAnswer": "Exact Option Text", "explanation": "reason"}
                """
        };
    }

    public static string BuildGenerationPrompt(int count, string difficultyPrompt, string typePrompt, string content)
    {
        return $$"""
        You are an expert tutor. Your task is to generate {{count}} high-quality study questions based on the provided content.

        ### Context
        The user is using an educational platform. The output MUST be a strict JSON object that will be parsed by a backend system. Any deviation from the schema will cause a system error.

        ### Difficulty
        {{difficultyPrompt}}

        ### Question Types and Schemas
        {{typePrompt}}

        ### Mandatory Output Format
        Your response must be a SINGLE JSON object with the following structure:
        {
          "questions": [
            {
              "text": "The prompt or question content",
              "type": "MultipleChoice" | "TrueFalse" | "OpenText" | "Matching" | "Ordering",
              "options": ["Option A", "Option B", ...], (Only for MC/TF/Ordering)
              "pairs": ["Term:Definition", ...], (Only for Matching)
              "correctAnswer": "The correct response or key",
              "explanation": "Why this is correct",
              "difficultyLevel": {{count}} (Use simple int 1-5, relative to requested difficulty)
            }
          ]
        }

        ### Rules
        1. NO conversational filler. NO markdown code blocks (unless the whole response is the block).
        2. Ensure valid JSON syntax.
        3. If type is Mixed, ensure you use a variety of the types mentioned above.
        
        Content:
        {{content}}
        """;
    }
}
