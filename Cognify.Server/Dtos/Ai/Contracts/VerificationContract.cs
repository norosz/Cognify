namespace Cognify.Server.Dtos.Ai.Contracts;

public record VerificationContractRequest(
    string ContractVersion,
    List<VerificationItem> Items,
    string? QuizRubric = null);

public record VerificationItem(
    Guid QuestionId,
    string QuestionPrompt,
    string UserAnswer,
    string CorrectAnswer,
    string QuestionType);

public record VerificationContractResponse(
    string ContractVersion,
    List<VerificationResult> Results);

public record VerificationResult(
    Guid QuestionId,
    double Score,
    bool IsCorrect,
    string? Feedback);
