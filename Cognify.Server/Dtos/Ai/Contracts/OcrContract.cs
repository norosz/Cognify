namespace Cognify.Server.Dtos.Ai.Contracts;

public record OcrContractRequest(
    string ContractVersion,
    string ContentType,
    string? Language,
    IReadOnlyList<string>? Hints);

public record OcrContractResponse(
    string ContractVersion,
    string ExtractedText,
    string? BlocksJson,
    double? Confidence);
