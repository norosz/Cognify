using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Dtos.Categories;
using Cognify.Server.Dtos.Ai;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai;

namespace Cognify.Server.Services.Interfaces;

public interface IAiService
{
    Task<List<GeneratedQuestion>> GenerateQuestionsAsync(string content, QuestionType type, int difficulty, int count);
    Task<QuizGenerationContractResponse> GenerateQuizAsync(QuizGenerationContractRequest request);
    Task<QuizGenerationContractResponse> RepairQuizAsync(QuizRepairContractRequest request);
    Task<string> ParseHandwritingAsync(Stream imageStream, string contentType);
    Task<OcrContractResponse> ParseHandwritingAsync(Stream imageStream, OcrContractRequest request);
    Task<string> GradeAnswerAsync(string question, string answer, string context);
    Task<GradingContractResponse> GradeAnswerAsync(GradingContractRequest request);
    Task<ExplainMistakeResponse> ExplainMistakeAsync(ExplainMistakeRequest request);
    Task<CategorySuggestionResponse> SuggestCategoriesAsync(string context, int maxSuggestions);
}
