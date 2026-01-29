namespace Cognify.Server.Dtos.Ai.Contracts;

public record GradingContractRequest(
    string ContractVersion,
    string QuestionPrompt,
    string UserAnswer,
    string AnswerKey,
    string? Rubric,
    string? KnownMistakePatterns);

public record GradingContractResponse(
    string ContractVersion,
    double Score,
    double MaxScore,
    string? Feedback,
    IReadOnlyList<string>? DetectedMistakes,
    double? ConfidenceEstimate,
    string? RawAnalysis);
