using Cognify.Server.Models;
using Cognify.Server.Models.Ai;

namespace Cognify.Server.Dtos.Ai.Contracts;

public record QuizGenerationContractRequest(
    string ContractVersion,
    string NoteContent,
    QuestionType QuestionType,
    int Difficulty,
    int QuestionCount,
    string? KnowledgeStateSnapshot,
    string? MistakeFocus);

public record QuizGenerationContractResponse(
    string ContractVersion,
    List<GeneratedQuestion> Questions,
    string? QuizRubric);
