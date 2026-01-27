namespace Cognify.Server.Dtos;

public record ExtractedContentDto(
    Guid Id,
    Guid DocumentId,
    Guid ModuleId,
    string DocumentName,
    string ModuleName,
    string Text,
    DateTime ExtractedAt,
    string Status,
    string? ErrorMessage
);

public record SaveAsNoteRequest(string Title);
