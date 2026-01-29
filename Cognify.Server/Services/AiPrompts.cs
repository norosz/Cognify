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
            QuestionType.MultipleSelect => """
                Type: MultipleSelect.
                Schema: {"text": "Select all that apply", "type": "MultipleSelect", "options": ["Option 1", "Option 2", "Option 3", "Option 4"], "correctAnswer": "Option 1|Option 3", "explanation": "Multiple options are correct"}
                """,
            QuestionType.Mixed => """
                Type: Mixed. Use a variety of MultipleChoice, TrueFalse, Matching, Ordering, and MultipleSelect.
                Schema: Use the appropriate schema for each type. Ensure "type" property reflects the chosen type.
                """,
            _ => """
                Type: MultipleChoice.
                Schema: {"text": "question", "type": "MultipleChoice", "options": ["A", "B", "C", "D"], "correctAnswer": "Exact Option Text", "explanation": "reason"}
                """
        };
    }

    public static string BuildGenerationPrompt(int count, int difficultyLevel, string difficultyPrompt, string typePrompt, string content)
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
              "type": "MultipleChoice" | "TrueFalse" | "OpenText" | "Matching" | "Ordering" | "MultipleSelect",
              "options": ["Option A", "Option B", ...], (For MC/TF/Ordering/MultipleSelect)
              "pairs": ["Term:Definition", ...], (Only for Matching)
              "correctAnswer": "The correct response or key",
              "explanation": "Why this is correct",
              "difficultyLevel": {{difficultyLevel}} (Use simple int 1-5, relative to requested difficulty)
            }
          ]
        }

        ### Rules
        1. NO conversational filler. DO NOT wrap the JSON output in markdown code blocks (e.g., ```json ... ```). Return raw JSON only.
        2. Ensure valid JSON syntax.
        3. MATHEMATICAL FORMULAS: Always use LaTeX for math, variables, and formulas to ensure beautiful rendering.
           - Use $...$ for inline math (e.g., $x^2 + y^2 = z^2$).
           - Use $$...$$ for display math on its own line. DO NOT use [ ] or ( ) for math.
           - For single variables or symbols, ALWAYS use LaTeX (e.g., use $t$ instead of 't').
           - Example CORRECT: "Solve for $x$ where $x^2 - 4 = 0$."
           - Example INCORRECT: "Solve for x where x^2 - 4 = 0."
        4. TECHNICAL TERMS: Use Markdown backticks (e.g., `i.true`) for code snippets, identifiers, or keywords that are not mathematical.
        5. If type is Mixed, ensure you use a variety of the types mentioned above.
        
        Content:
        {{content}}
        """;
    }

        public static string BuildGenerationPromptWithRubric(
                int count,
                int difficultyLevel,
                string difficultyPrompt,
                string typePrompt,
                string content,
                string? knowledgeStateSnapshot,
                string? mistakeFocus)
        {
                return $$"""
                You are an expert tutor. Your task is to generate {{count}} high-quality study questions based on the provided content.

                ### Context
                The user is using an educational platform. The output MUST be a strict JSON object that will be parsed by a backend system. Any deviation from the schema will cause a system error.

                ### Difficulty
                {{difficultyPrompt}}

                ### Question Types and Schemas
                {{typePrompt}}

                ### Learner State (if provided)
                {{knowledgeStateSnapshot ?? "(none)"}}

                ### Mistake Focus (if provided)
                {{mistakeFocus ?? "(none)"}}

                ### Mandatory Output Format
                Your response must be a SINGLE JSON object with the following structure:
                {
                    "questions": [
                        {
                            "text": "The prompt or question content",
                            "type": "MultipleChoice" | "TrueFalse" | "OpenText" | "Matching" | "Ordering" | "MultipleSelect",
                            "options": ["Option A", "Option B", ...], (For MC/TF/Ordering/MultipleSelect)
                            "pairs": ["Term:Definition", ...], (Only for Matching)
                            "correctAnswer": "The correct response or key",
                            "explanation": "Why this is correct",
                            "difficultyLevel": {{difficultyLevel}} (Use simple int 1-5, relative to requested difficulty)
                        }
                    ],
                    "quizRubric": "Concise rubric describing how to score open-text answers and what key concepts must appear."
                }

                ### Rules
                1. NO conversational filler. DO NOT wrap the JSON output in markdown code blocks (e.g., ```json ... ```). Return raw JSON only.
                2. Ensure valid JSON syntax.
                3. MATHEMATICAL FORMULAS: Always use LaTeX for math, variables, and formulas to ensure beautiful rendering.
                     - Use $...$ for inline math (e.g., $x^2 + y^2 = z^2$).
                     - Use $$...$$ for display math on its own line. DO NOT use [ ] or ( ) for math.
                     - For single variables or symbols, ALWAYS use LaTeX (e.g., use $t$ instead of 't').
                     - Example CORRECT: "Solve for $x$ where $x^2 - 4 = 0$."
                     - Example INCORRECT: "Solve for x where x^2 - 4 = 0."
                4. TECHNICAL TERMS: Use Markdown backticks (e.g., `i.true`) for code snippets, identifiers, or keywords that are not mathematical.
                5. If type is Mixed, ensure you use a variety of the types mentioned above.

                Content:
                {{content}}
                """;
        }

    public static string BuildGradingPromptWithRubric(
        string question,
        string answer,
        string context,
        string? knownMistakePatterns)
    {
        return $$"""
        You are an expert grading assistant. Grade the student's answer against the context and rubric.

        ### Input
        Question: {{question}}
        Student Answer: {{answer}}
        Context/Correct Answer Key: {{context}}
        Known Mistake Patterns (if any): {{knownMistakePatterns ?? "(none)"}}

        ### Mandatory Output Format
        Return a SINGLE JSON object with the following structure:
        {
          "score": 0-100,
          "feedback": "Concise feedback with actionable guidance",
          "detectedMistakes": ["MistakeCategory1", "MistakeCategory2"],
          "confidenceEstimate": 0.0-1.0
        }

        ### Rules
        1. NO conversational filler. DO NOT wrap the JSON output in markdown code blocks.
        2. Ensure valid JSON syntax.
        3. Use LaTeX for math expressions ($...$). Use Markdown for emphasis where helpful.
        """;
    }
}
