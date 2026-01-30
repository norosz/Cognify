using Cognify.Server.Models.Ai;

namespace Cognify.Server.Dtos.Ai.Contracts;

public record QuizRepairContractRequest(
    string ContractVersion,
    List<GeneratedQuestion> Questions,
    string? QuizRubric);
