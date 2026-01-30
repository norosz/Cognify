using Cognify.Server.Models;

namespace Cognify.Server.Dtos;

public record PendingQuizDto(
    Guid Id,
    Guid? NoteId,
    Guid ModuleId,
    string Title,
    string NoteName,
    string ModuleName,
    string Difficulty,
    string QuestionType,
    int QuestionCount,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt
);

public record CreatePendingQuizRequest(
    Guid NoteId,
    string Title,
    string Difficulty, // "Beginner", "Intermediate", "Advanced"
    string QuestionType, // "MultipleChoice", "TrueFalse", "OpenText", "Matching", "Mixed"
    int QuestionCount
);
