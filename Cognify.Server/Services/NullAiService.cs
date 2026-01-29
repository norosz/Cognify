using Cognify.Server.Dtos.Ai.Contracts;
using Cognify.Server.Models;
using Cognify.Server.Models.Ai;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class NullAiService : IAiService
{
    public Task<List<GeneratedQuestion>> GenerateQuestionsAsync(string content, QuestionType type, int difficulty, int count)
    {
        return Task.FromResult(new List<GeneratedQuestion>());
    }

    public Task<QuizGenerationContractResponse> GenerateQuizAsync(QuizGenerationContractRequest request)
    {
        return Task.FromResult(new QuizGenerationContractResponse(request.ContractVersion, [], QuizRubric: null));
    }

    public Task<string> ParseHandwritingAsync(Stream imageStream, string contentType)
    {
        return Task.FromResult(string.Empty);
    }

    public Task<OcrContractResponse> ParseHandwritingAsync(Stream imageStream, OcrContractRequest request)
    {
        return Task.FromResult(new OcrContractResponse(request.ContractVersion, string.Empty, BlocksJson: null, Confidence: null));
    }

    public Task<string> GradeAnswerAsync(string question, string answer, string context)
    {
        return Task.FromResult("Score: 0\n\nFeedback: AI grading disabled.");
    }

    public Task<GradingContractResponse> GradeAnswerAsync(GradingContractRequest request)
    {
        return Task.FromResult(new GradingContractResponse(
            request.ContractVersion,
            0,
            100,
            "AI grading disabled.",
            DetectedMistakes: null,
            ConfidenceEstimate: 0,
            RawAnalysis: null));
    }
}
